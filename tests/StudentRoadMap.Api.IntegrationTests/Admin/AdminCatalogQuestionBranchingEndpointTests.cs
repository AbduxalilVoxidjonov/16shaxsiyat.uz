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
using StudentRoadMap.Domain.Catalog.Branching;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `POST/PUT .../questions` yangi maydonlari (`sectionCode`, `placeholder`, `inputPattern`,
/// `maxLength`, `visibility`, `options[]`) va B-1/B-2 qo'riqchilari — `docs/18` §5. Alohida
/// `IClassFixture` (login kvotasi, `AdminCatalogSectionsEndpointTests`dagi izohga qarang).
/// </summary>
public sealed class AdminCatalogQuestionBranchingEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminCatalogQuestionBranchingEndpointTests(PublicApiTestFactory factory)
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
    public async Task CreateQuestion_ScoredAnketadaShortText_400QUESTION_TYPE_NOT_SCORABLEQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("catalog-b1-scored-admin");
        var created = await client.PostAsJsonAsync(
            "/api/admin/catalog/tests",
            new { code = "SCORED-B1-1", nameUz = "Ballanadigan anketa", estimatedMinutes = 5 },
            TestJson.Options);
        var test = await created.Content.ReadFromJsonAsync<CatalogTestDetailDto>(TestJson.Options);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/catalog/tests/{test!.Id}/questions",
            new { code = "B1-Q1", order = 1, textUz = "Matn savoli", type = "ShortText", scale = "GEN", direction = 1, weight = 1.0m },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("QUESTION_TYPE_NOT_SCORABLE");
    }

    [Fact]
    public async Task CreateQuestion_ScoredAnketadaVisibility_400BRANCHING_NOT_ALLOWED_IN_SCOREDQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("catalog-b2-scored-admin");
        var created = await client.PostAsJsonAsync(
            "/api/admin/catalog/tests",
            new { code = "SCORED-B2-1", nameUz = "Ballanadigan anketa 2", estimatedMinutes = 5 },
            TestJson.Options);
        var test = await created.Content.ReadFromJsonAsync<CatalogTestDetailDto>(TestJson.Options);

        var visibility = new VisibilityRule(VisibilityMatch.All, [new VisibilityCondition("B2-Q1", VisibilityOperator.Equals, [1])]);
        var response = await client.PostAsJsonAsync(
            $"/api/admin/catalog/tests/{test!.Id}/questions",
            new { code = "B2-Q2", order = 2, textUz = "Shartli savol", type = "Likert5", scale = "GEN", direction = 1, weight = 1.0m, visibility },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("BRANCHING_NOT_ALLOWED_IN_SCORED");
    }

    [Fact]
    public async Task CreateQuestion_YangiMaydonlarBilan_TolaSaqlanadi()
    {
        using var client = await AuthenticatedClientAsync("catalog-create-fields-admin");
        var testId = await CreateSurveyTestAsync(client, "CREATE-FIELDS-1");

        var response = await client.PostAsJsonAsync(
            $"/api/admin/catalog/tests/{testId}/questions",
            new
            {
                code = "FIELDS-Q1",
                order = 1,
                textUz = "Telefon raqamingiz",
                type = "Phone",
                scale = "SURVEY",
                direction = 1,
                weight = 1.0m,
                placeholder = "+998 90 123 45 67",
                inputPattern = "^\\+?998[0-9]{9}$",
                maxLength = 20,
            },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CatalogQuestionItemDto>(TestJson.Options);
        body!.Placeholder.Should().Be("+998 90 123 45 67");
        body.InputPattern.Should().Be("^\\+?998[0-9]{9}$");
        body.MaxLength.Should().Be(20);
    }

    [Fact]
    public async Task CreateQuestion_YaroqsizInputPattern_400INPUT_PATTERN_INVALIDQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("catalog-invalid-pattern-admin");
        var testId = await CreateSurveyTestAsync(client, "INVALID-PATTERN-1");

        var response = await client.PostAsJsonAsync(
            $"/api/admin/catalog/tests/{testId}/questions",
            new { code = "PATTERN-Q1", order = 1, textUz = "Savol", type = "ShortText", scale = "SURVEY", direction = 1, weight = 1.0m, inputPattern = "(unbalanced[" },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("INPUT_PATTERN_INVALID");
    }

    [Fact]
    public async Task UpdateQuestion_OptionsToLiqAlmashtiradi()
    {
        using var client = await AuthenticatedClientAsync("catalog-options-replace-admin");
        var testId = await CreateSurveyTestAsync(client, "OPTIONS-REPLACE-1");

        var created = await client.PostAsJsonAsync(
            $"/api/admin/catalog/tests/{testId}/questions",
            new
            {
                code = "OPTIONS-Q1",
                order = 1,
                textUz = "Variant tanlang",
                type = "SingleChoice",
                scale = "SURVEY",
                direction = 1,
                weight = 1.0m,
                options = new[] { new { textUz = "A", value = 1, displayOrder = 1 }, new { textUz = "B", value = 2, displayOrder = 2 } },
            },
            TestJson.Options);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var question = await created.Content.ReadFromJsonAsync<CatalogQuestionItemDto>(TestJson.Options);
        question!.Options.Should().HaveCount(2);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/admin/catalog/questions/{question.Id}",
            new
            {
                textUz = "Variant tanlang",
                isActive = true,
                options = new[] { new { textUz = "C", value = 3, displayOrder = 1 } },
            },
            TestJson.Options);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CatalogQuestionItemDto>(TestJson.Options);
        updated!.Options.Should().ContainSingle(o => o.TextUz == "C" && o.Value == 3);
    }

    [Fact]
    public async Task UpdateQuestion_TizimSavolidaYangiMaydonlarBerilsaHamFaqatMatnOzgaradi()
    {
        using var client = await AuthenticatedClientAsync("catalog-system-update-ignores-admin");
        Guid systemQuestionId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;
            var testId = Guid.NewGuid();
            var question = Question.Create(Guid.NewGuid(), testId, "SYS-UPD-Q1", 1, "Tizim savoli", QuestionType.Likert5, "EI", 1, 1.0m, isSystem: true);
            var systemTest = TestDefinition.CreateSystemPublished(
                testId, "SYS-UPD-1", "Tizim testi", null, 1, 5, false, 10, "MBTI16", [question], now);
            db.TestDefinitions.Add(systemTest);
            await db.SaveChangesAsync();
            systemQuestionId = question.Id;
        }

        var response = await client.PutAsJsonAsync(
            $"/api/admin/catalog/questions/{systemQuestionId}",
            new { textUz = "Yangilangan matn", isActive = true, placeholder = "Bu e'tiborsiz qoldirilishi kerak" },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CatalogQuestionItemDto>(TestJson.Options);
        body!.TextUz.Should().Be("Yangilangan matn");
        body.Placeholder.Should().BeNull("BR-8: tizim savolida faqat textUz/textRu/isActive o'zgaradi");
    }
}
