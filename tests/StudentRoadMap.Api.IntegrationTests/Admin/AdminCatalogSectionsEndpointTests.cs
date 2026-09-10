using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `POST/GET/PUT/DELETE .../sections` + reorder — `docs/18` §5. Alohida `IClassFixture`
/// (login/`AdminLogin` kvotasi 10/5 daqiqa, IP bo'yicha, `AdminCatalogEndpointTests`dagi izohga
/// qarang) — har `[Fact]` o'z login chaqiruviga ega, shu sabab sinf ichidagi testlar soni
/// kamida bittasi ishlatadigan kvotadan past bo'lishi kerak.
/// </summary>
public sealed class AdminCatalogSectionsEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminCatalogSectionsEndpointTests(PublicApiTestFactory factory)
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

    private static async Task<Guid> CreateSurveyTestAsync(HttpClient client, string code)
    {
        var response = await client.PostAsJsonAsync(
            "/api/admin/catalog/tests",
            new { code, nameUz = $"{code} nomi", estimatedMinutes = 5, scoringMode = "Survey", pageSize = 60 },
            TestJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var test = await response.Content.ReadFromJsonAsync<CatalogTestDetailDto>(TestJson.Options);
        return test!.Id;
    }

    [Fact]
    public async Task Sections_CreateListUpdateDelete_ToGriIshlaydi()
    {
        using var client = await AuthenticatedClientAsync("catalog-sections-crud-admin");
        var testId = await CreateSurveyTestAsync(client, "SECTIONS-CRUD-1");

        var createResponse = await client.PostAsJsonAsync(
            $"/api/admin/catalog/tests/{testId}/sections",
            new { code = "S1", titleUz = "Birinchi bo'lim", descriptionUz = (string?)null, displayOrder = 1, visibility = (object?)null },
            TestJson.Options);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CatalogSectionItemDto>(TestJson.Options);
        created!.Code.Should().Be("S1");

        var listResponse = await client.GetAsync(new Uri($"/api/admin/catalog/tests/{testId}/sections", UriKind.Relative));
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<List<CatalogSectionItemDto>>(TestJson.Options);
        list.Should().ContainSingle(s => s.Code == "S1");

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/admin/catalog/sections/{created.Id}",
            new { titleUz = "Yangilangan sarlavha", descriptionUz = "Tavsif", visibility = (object?)null },
            TestJson.Options);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CatalogSectionItemDto>(TestJson.Options);
        updated!.TitleUz.Should().Be("Yangilangan sarlavha");

        var deleteResponse = await client.DeleteAsync(new Uri($"/api/admin/catalog/sections/{created.Id}", UriKind.Relative));
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listAfterDelete = await client.GetAsync(new Uri($"/api/admin/catalog/tests/{testId}/sections", UriKind.Relative));
        var itemsAfterDelete = await listAfterDelete.Content.ReadFromJsonAsync<List<CatalogSectionItemDto>>(TestJson.Options);
        itemsAfterDelete.Should().BeEmpty();
    }

    [Fact]
    public async Task Sections_Reorder_DisplayOrderniYangilaydi()
    {
        using var client = await AuthenticatedClientAsync("catalog-sections-reorder-admin");
        var testId = await CreateSurveyTestAsync(client, "SECTIONS-REORDER-1");

        var first = await client.PostAsJsonAsync($"/api/admin/catalog/tests/{testId}/sections", new { code = "S1", titleUz = "S1", displayOrder = 1 }, TestJson.Options);
        var second = await client.PostAsJsonAsync($"/api/admin/catalog/tests/{testId}/sections", new { code = "S2", titleUz = "S2", displayOrder = 2 }, TestJson.Options);
        var firstSection = await first.Content.ReadFromJsonAsync<CatalogSectionItemDto>(TestJson.Options);
        var secondSection = await second.Content.ReadFromJsonAsync<CatalogSectionItemDto>(TestJson.Options);

        var reorderResponse = await client.PostAsJsonAsync(
            $"/api/admin/catalog/tests/{testId}/sections/reorder",
            new { items = new[] { new { id = firstSection!.Id, displayOrder = 20 }, new { id = secondSection!.Id, displayOrder = 10 } } },
            TestJson.Options);

        reorderResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reordered = await reorderResponse.Content.ReadFromJsonAsync<List<CatalogSectionItemDto>>(TestJson.Options);
        reordered!.Select(s => s.Code).Should().Equal("S2", "S1");
    }

    [Fact]
    public async Task Sections_SavollariBorBolimniOchirish_409SECTION_IN_USEQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("catalog-section-in-use-admin");
        var testId = await CreateSurveyTestAsync(client, "SECTION-INUSE-1");

        var sectionResponse = await client.PostAsJsonAsync($"/api/admin/catalog/tests/{testId}/sections", new { code = "S1", titleUz = "S1", displayOrder = 1 }, TestJson.Options);
        var section = await sectionResponse.Content.ReadFromJsonAsync<CatalogSectionItemDto>(TestJson.Options);

        var questionResponse = await client.PostAsJsonAsync(
            $"/api/admin/catalog/tests/{testId}/questions",
            new { code = "SEC-INUSE-Q1", order = 1, textUz = "Savol", type = "ShortText", scale = "SURVEY", direction = 1, weight = 1.0m, sectionCode = "S1" },
            TestJson.Options);
        questionResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var deleteResponse = await client.DeleteAsync(new Uri($"/api/admin/catalog/sections/{section!.Id}", UriKind.Relative));

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await deleteResponse.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("SECTION_IN_USE");
    }

    [Fact]
    public async Task Sections_TakrorlanganKod_409SECTION_CODE_DUPLICATEQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("catalog-section-dupe-admin");
        var testId = await CreateSurveyTestAsync(client, "SECTION-DUPE-1");

        await client.PostAsJsonAsync($"/api/admin/catalog/tests/{testId}/sections", new { code = "S1", titleUz = "S1", displayOrder = 1 }, TestJson.Options);
        var response = await client.PostAsJsonAsync($"/api/admin/catalog/tests/{testId}/sections", new { code = "S1", titleUz = "S1 (dublikat)", displayOrder = 2 }, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("SECTION_CODE_DUPLICATE");
    }

    [Fact]
    public async Task Sections_TizimTestidaYaratish_409SYSTEM_TEST_LOCKEDQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("catalog-section-system-locked-admin");
        Guid systemTestId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;
            var testId = Guid.NewGuid();
            var question = Question.Create(Guid.NewGuid(), testId, "SYS-SEC-Q1", 1, "Tizim savoli", QuestionType.Likert5, "EI", 1, 1.0m, isSystem: true);
            var systemTest = TestDefinition.CreateSystemPublished(
                testId, "SYS-SEC-1", "Tizim testi", null, 1, 5, false, 10, "MBTI16", [question], now);
            db.TestDefinitions.Add(systemTest);
            await db.SaveChangesAsync();
            systemTestId = testId;
        }

        var response = await client.PostAsJsonAsync(
            $"/api/admin/catalog/tests/{systemTestId}/sections",
            new { code = "S1", titleUz = "Bo'lim", displayOrder = 1 },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("SYSTEM_TEST_LOCKED");
    }
}
