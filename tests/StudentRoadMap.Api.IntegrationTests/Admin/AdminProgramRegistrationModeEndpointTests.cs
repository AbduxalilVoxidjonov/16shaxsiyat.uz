using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// P52 (2026-09-11, egasining qarori): `AssessmentProgram.RegistrationMode` — admin API
/// (`docs/18-tarmoqlanuvchi-sorovnoma.md` §9). Alohida `IClassFixture` —
/// `AdminAssessmentProgramsEndpointTests` bilan bitta klassda `AdminLogin` rate limiteri
/// (10/5 daqiqa/IP) bo'linib ketmasligi uchun.
/// </summary>
public sealed class AdminProgramRegistrationModeEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminProgramRegistrationModeEndpointTests(PublicApiTestFactory factory)
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

    [Fact]
    public async Task Create_RegistrationModeBerilmasa_FullQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("programs-regmode-default-admin");

        var response = await client.PostAsJsonAsync(
            "/api/admin/programs",
            new { code = "REGMODE-DEFAULT-1", nameUz = "Standart rejim dasturi", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned" },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        body!.RegistrationMode.Should().Be("Full");
    }

    [Fact]
    public async Task Create_RegistrationModeNone_QaytaSaqlanadi()
    {
        using var client = await AuthenticatedClientAsync("programs-regmode-none-admin");

        var response = await client.PostAsJsonAsync(
            "/api/admin/programs",
            new { code = "REGMODE-NONE-1", nameUz = "Ro'yxatdan o'tishsiz dastur", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned", registrationMode = "None" },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        body!.RegistrationMode.Should().Be("None");
    }

    [Fact]
    public async Task Update_RegistrationModeNoneToFull_BatareyasizDastur_Succeeds()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var test = await TestDataFactory.CreateStandaloneRiasecShapedTestAsync(db, now, "REGMODEUPD1", 1);

        using var client = await AuthenticatedClientAsync("programs-regmode-update-admin");
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/programs",
            new { code = "REGMODE-UPD-1", nameUz = "Yangilanadigan dastur", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned", registrationMode = "None" },
            TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);

        await client.PostAsJsonAsync($"/api/admin/programs/{created!.Id}/tests", new { testDefinitionId = test.Id, displayOrder = 1 }, TestJson.Options);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/admin/programs/{created.Id}",
            new { nameUz = created.NameUz, descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned", registrationMode = "Full" },
            TestJson.Options);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await updateResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        body!.RegistrationMode.Should().Be("Full");
    }

    /// <summary>
    /// Qat'iy invariant (egasining qarori): shaxsiyat batareyasi (`Standard` + `Scored`
    /// metodika, masalan `MBTI16`) bor dasturga `None` qo'yib bo'lmaydi — BIRINCHI nazorat
    /// nuqtasi (`SetRegistrationMode`).
    /// </summary>
    [Fact]
    public async Task Update_RegistrationModeNone_BatareyaliDasturda_400REGISTRATION_REQUIRED_FOR_BATTERYQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var mbtiTest = await TestDataFactory.CreateStandaloneSystemMbtiShapedTestAsync(db, now, "REGMODEBAT1", 1);

        using var client = await AuthenticatedClientAsync("programs-regmode-battery-admin");
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/programs",
            new { code = "REGMODE-BAT-1", nameUz = "Batareyali dastur", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned" },
            TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);

        await client.PostAsJsonAsync($"/api/admin/programs/{created!.Id}/tests", new { testDefinitionId = mbtiTest.Id, displayOrder = 1 }, TestJson.Options);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/admin/programs/{created.Id}",
            new { nameUz = created.NameUz, descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned", registrationMode = "None" },
            TestJson.Options);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("REGISTRATION_REQUIRED_FOR_BATTERY");

        // Muvaffaqiyatsiz urinishdan keyin ham dastur `Full` bo'lib qolishi kerak.
        var afterFailedUpdate = await client.GetFromJsonAsync<AdminProgramDetailDto>(
            new Uri($"/api/admin/programs/{created.Id}", UriKind.Relative), TestJson.Options);
        afterFailedUpdate!.RegistrationMode.Should().Be("Full");
    }

    /// <summary>IKKINCHI nazorat nuqtasi — `Publish`da ham qulflangan (`SetRegistrationMode`ni chetlab o'tib bo'lmaydi).</summary>
    [Fact]
    public async Task AddTest_BatareyaAnketasiNoneRejimigaBiriktirilsa_400REGISTRATION_REQUIRED_FOR_BATTERYQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var mbtiTest = await TestDataFactory.CreateStandaloneSystemMbtiShapedTestAsync(db, now, "REGMODEPUB1", 1);

        using var client = await AuthenticatedClientAsync("programs-regmode-publish-admin");
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/programs",
            new { code = "REGMODE-PUB-1", nameUz = "Nashr sinovi dasturi", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned", registrationMode = "None" },
            TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);

        // Yuqoridagi `AdminProgramRegistrationFieldsEndpointTests`dagi bilan bir xil sabab:
        // batareya anketasi `RegistrationMode = None` dasturga BIRIKTIRILAYOTGAN paytda rad
        // etiladi, nashrni kutmaydi. Nashr qulfi domen testida saqlanadi
        // (`AssessmentProgramTests.Publish_NoneModeWithBattery_ThrowsDomainException`).
        var addTestResponse = await client.PostAsJsonAsync(
            $"/api/admin/programs/{created!.Id}/tests",
            new { testDefinitionId = mbtiTest.Id, displayOrder = 1 },
            TestJson.Options);

        addTestResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await addTestResponse.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("REGISTRATION_REQUIRED_FOR_BATTERY");
    }
}
