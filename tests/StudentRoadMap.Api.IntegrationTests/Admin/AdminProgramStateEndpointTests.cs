using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// Dasturning YAGONA holati (`ProgramState`: `Draft` · `Active` · `Paused` · `Archived`) —
/// 2026-09-06. Egasi ro'yxatda bitta dasturni bir vaqtda "Arxiv" ham, "Faol" ham bo'lib
/// ko'rgan edi: `Status` va `IsActive` ikkita mustaqil ustun/belgi edi va `Activate()`
/// holatni umuman tekshirmasdi.
///
/// **Nega alohida sinf:** `AdminLogin` limiti IP bo'yicha 10/5 daqiqa (`RateLimitSetup`),
/// `AdminAssessmentProgramsEndpointTests` esa allaqachon shu chegaraga yaqin. Har sinf o'z
/// `PublicApiTestFactory` nusxasini (o'z bazasi va o'z limiteri bilan) oladi.
/// </summary>
public sealed class AdminProgramStateEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminProgramStateEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string username)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await AdminTestDataFactory.CreateAdminUserAsync(db, hasher, DateTimeOffset.UtcNow, username);
        }

        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResult>(TestJson.Options))!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    /// <summary>
    /// Egasi ko'rgan xatoning ildizi: arxivlangan dasturni "faollashtirib" bo'lardi va u
    /// ro'yxatda bir vaqtda "Arxiv" ham, "Faol" ham bo'lib ko'rinardi. Endi domen buni
    /// taqiqlaydi, API esa `409 PROGRAM_INVALID_TRANSITION` qaytaradi.
    /// </summary>
    [Fact]
    public async Task ToggleActive_ArxivlanganDastur_409PROGRAM_INVALID_TRANSITIONQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("programs-toggle-archived-admin");
        var created = await CreateArchivedProgramAsync(client, "TOGGLE-ARCHIVED-1", "ARCHTG1");

        var response = await client.PostAsync(
            new Uri($"/api/admin/programs/{created.Id}/toggle-active", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("PROGRAM_INVALID_TRANSITION");

        // Holat o'zgarmagan: qator hamon FAQAT "Arxiv".
        var after = await client.GetFromJsonAsync<AdminProgramDetailDto>(
            $"/api/admin/programs/{created.Id}", TestJson.Options);
        after!.State.Should().Be("Archived");
    }

    /// <summary>Qoralama dasturni ham "to'xtatib"/"faollashtirib" bo'lmaydi.</summary>
    [Fact]
    public async Task ToggleActive_QoralamaDastur_409PROGRAM_INVALID_TRANSITIONQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("programs-toggle-draft-admin");
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/programs",
            new { code = "TOGGLE-DRAFT-1", nameUz = "Qoralama dastur", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned" },
            TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        created!.State.Should().Be("Draft");

        var response = await client.PostAsync(
            new Uri($"/api/admin/programs/{created.Id}/toggle-active", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("PROGRAM_INVALID_TRANSITION");
    }

    /// <summary>
    /// Nashr qilingan dasturni to'xtatish — `Active ──▶ Paused`. Ilgari bu ikkita mustaqil
    /// maydon edi va UI'da ikkita belgi ("Nashr etilgan" + "Nofaol") ko'rinardi.
    /// </summary>
    [Fact]
    public async Task ToggleActive_NashrQilinganDastur_PausedVaQaytaActiveBoladi()
    {
        using var client = await AuthenticatedClientAsync("programs-toggle-published-admin");
        var created = await CreatePublishedProgramAsync(client, "TOGGLE-PUB-1", "TGLPUB1");

        var paused = await (await client.PostAsync(
                new Uri($"/api/admin/programs/{created.Id}/toggle-active", UriKind.Relative), content: null))
            .Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        paused!.State.Should().Be("Paused");

        var active = await (await client.PostAsync(
                new Uri($"/api/admin/programs/{created.Id}/toggle-active", UriKind.Relative), content: null))
            .Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        active!.State.Should().Be("Active");
    }

    /// <summary>
    /// BITTA `state` filtri ikkita eski filtrning (`status` + `isActive`) o'rnini bosadi —
    /// `Active` va `Paused` bir-biridan ajraladi, ziddiyatli juftlik tanlab bo'lmaydi.
    /// </summary>
    [Fact]
    public async Task List_StateFiltri_HarBirHolatniAjratadi()
    {
        using var client = await AuthenticatedClientAsync("programs-state-filter-admin");

        var draft = await (await client.PostAsJsonAsync(
                "/api/admin/programs",
                new { code = "STATE-DRAFT-1", nameUz = "Holat: qoralama", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned" },
                TestJson.Options))
            .Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);

        var active = await CreatePublishedProgramAsync(client, "STATE-ACTIVE-1", "STACT1");
        var paused = await CreatePublishedProgramAsync(client, "STATE-PAUSED-1", "STPAU1");
        await client.PostAsync(new Uri($"/api/admin/programs/{paused.Id}/toggle-active", UriKind.Relative), content: null);
        var archived = await CreateArchivedProgramAsync(client, "STATE-ARCHIVED-1", "STARC1");

        await AssertOnlyStateAsync(client, "Draft", draft!.Id, [active.Id, paused.Id, archived.Id]);
        await AssertOnlyStateAsync(client, "Active", active.Id, [draft.Id, paused.Id, archived.Id]);
        await AssertOnlyStateAsync(client, "Paused", paused.Id, [draft.Id, active.Id, archived.Id]);
        await AssertOnlyStateAsync(client, "Archived", archived.Id, [draft.Id, active.Id, paused.Id]);
    }

    private static async Task AssertOnlyStateAsync(
        HttpClient client, string state, Guid expectedId, IReadOnlyCollection<Guid> unexpectedIds)
    {
        var page = await client.GetFromJsonAsync<PagedResult<AdminProgramListItemDto>>(
            $"/api/admin/programs?state={state}&pageSize=100", TestJson.Options);

        page!.Items.Should().Contain(p => p.Id == expectedId, $"'{state}' filtri shu dasturni qaytarishi kerak");
        page.Items.Should().NotContain(p => unexpectedIds.Contains(p.Id), $"'{state}' filtri boshqa holatdagi dasturni qaytarmasligi kerak");
        page.Items.Should().OnlyContain(p => p.State == state);
    }

    private async Task<AdminProgramDetailDto> CreatePublishedProgramAsync(HttpClient client, string programCode, string testCode)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var test = await TestDataFactory.CreateStandaloneRiasecShapedTestAsync(db, DateTimeOffset.UtcNow, testCode, 1);

        var created = await (await client.PostAsJsonAsync(
                "/api/admin/programs",
                new { code = programCode, nameUz = programCode, descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned" },
                TestJson.Options))
            .Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);

        await client.PostAsJsonAsync(
            $"/api/admin/programs/{created!.Id}/tests",
            new { testDefinitionId = test.Id, displayOrder = 1 },
            TestJson.Options);

        var published = await (await client.PostAsync(
                new Uri($"/api/admin/programs/{created.Id}/publish", UriKind.Relative), content: null))
            .Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);

        return published!;
    }

    private async Task<AdminProgramDetailDto> CreateArchivedProgramAsync(HttpClient client, string programCode, string testCode)
    {
        var published = await CreatePublishedProgramAsync(client, programCode, testCode);

        var archived = await (await client.PostAsync(
                new Uri($"/api/admin/programs/{published.Id}/archive", UriKind.Relative), content: null))
            .Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);

        archived!.State.Should().Be("Archived");
        return archived;
    }
}
