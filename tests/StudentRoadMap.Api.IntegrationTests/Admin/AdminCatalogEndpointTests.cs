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
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `AssessmentCatalogController` — `docs/07` §3.4 (P37, `prompts/37-katalog-crud-backend.md`).
/// Alohida `IClassFixture` (rate limiter kvotasi, `CLAUDE.md` qoidasi).
/// </summary>
public sealed class AdminCatalogEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminCatalogEndpointTests(PublicApiTestFactory factory)
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

    /// <summary>Tizim metodikasini (`IsSystem = true`) to'g'ridan-to'g'ri DB'ga yozadi — `TestDataFactory`da bunday fabrika yo'q (barchasi `Custom`).</summary>
    private static async Task<TestDefinition> CreateSystemTestAsync(AppDbContext db, DateTimeOffset now, string code)
    {
        var testId = Guid.NewGuid();
        var question = Question.Create(Guid.NewGuid(), testId, $"{code}-Q1", 1, "Tizim savoli", QuestionType.Likert5, "EI", 1, 1.0m, isSystem: true);

        var test = TestDefinition.CreateSystemPublished(
            testId, code, $"{code} nomi", null, displayOrder: 1, estimatedMinutes: 5, shuffleQuestions: false, pageSize: 10,
            scoringStrategyCode: "MBTI16", questions: [question], now: now);

        db.TestDefinitions.Add(test);
        await db.SaveChangesAsync();

        return test;
    }

    [Fact]
    public async Task CreateTest_ToGriMalumot_201VaDraftCustomQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("catalog-create-admin");

        var request = new { code = "STRESS-1", nameUz = "Stressga chidamlilik", descriptionUz = (string?)null, estimatedMinutes = 6, pageSize = 10, shuffleQuestions = false, displayOrder = 5, scoringMode = (string?)null };

        var response = await client.PostAsJsonAsync("/api/admin/catalog/tests", request, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CatalogTestDetailDto>(TestJson.Options);
        body!.Status.Should().Be("Draft");
        body.Kind.Should().Be("Custom");
        body.IsSystem.Should().BeFalse();
        body.ScoringMode.Should().Be("Scored");
    }

    [Fact]
    public async Task Publish_TestSizAnketa_400TEST_NOT_PUBLISHABLEIssuesBilanQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("catalog-publish-empty-admin");
        var created = await client.PostAsJsonAsync(
            "/api/admin/catalog/tests",
            new { code = "EMPTY-1", nameUz = "Bo'sh anketa", estimatedMinutes = 5 },
            TestJson.Options);
        var test = await created.Content.ReadFromJsonAsync<CatalogTestDetailDto>(TestJson.Options);

        var response = await client.PostAsync(new Uri($"/api/admin/catalog/tests/{test!.Id}/publish", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("TEST_NOT_PUBLISHABLE");
        problem.GetProperty("issues").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateQuestion_TizimTestidaChaqirilsa_409SYSTEM_TEST_LOCKEDQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("catalog-system-lock-admin");
        TestDefinition systemTest;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            systemTest = await CreateSystemTestAsync(db, DateTimeOffset.UtcNow, "SYS-LOCK-1");
        }

        var request = new { code = "NEW-Q", order = 99, textUz = "Yangi savol", type = "Likert5", scale = "EI", direction = 1, weight = 1.0m, isRequired = (bool?)null };
        var response = await client.PostAsJsonAsync($"/api/admin/catalog/tests/{systemTest.Id}/questions", request, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("SYSTEM_TEST_LOCKED");
    }

    [Fact]
    public async Task DeleteTest_TizimTest_409SYSTEM_TEST_LOCKEDQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("catalog-delete-system-admin");
        TestDefinition systemTest;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            systemTest = await CreateSystemTestAsync(db, DateTimeOffset.UtcNow, "SYS-DEL-1");
        }

        var response = await client.DeleteAsync(new Uri($"/api/admin/catalog/tests/{systemTest.Id}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("SYSTEM_TEST_LOCKED");
    }

    [Fact]
    public async Task ImportQuestions_DublikatKod_409Qaytaradi()
    {
        using var client = await AuthenticatedClientAsync("catalog-import-dupe-admin");
        var created = await client.PostAsJsonAsync(
            "/api/admin/catalog/tests",
            new { code = "IMPORT-DUPE-1", nameUz = "Import test", estimatedMinutes = 5 },
            TestJson.Options);
        var test = await created.Content.ReadFromJsonAsync<CatalogTestDetailDto>(TestJson.Options);

        var questions = new[]
        {
            new { code = "IQ-1", order = 1, textUz = "Savol 1", type = "Likert5", scale = "STRESS", direction = 1, weight = 1.0m, isRequired = (bool?)null },
            new { code = "IQ-1", order = 2, textUz = "Savol 2 (dublikat kod)", type = "Likert5", scale = "STRESS", direction = 1, weight = 1.0m, isRequired = (bool?)null },
        };

        var response = await client.PostAsJsonAsync($"/api/admin/catalog/tests/{test!.Id}/questions/import", new { questions }, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ImportQuestions_ToGriJson_DraftHolatidaQoladiVaSavollarQoshiladi()
    {
        using var client = await AuthenticatedClientAsync("catalog-import-ok-admin");
        var created = await client.PostAsJsonAsync(
            "/api/admin/catalog/tests",
            new { code = "IMPORT-OK-1", nameUz = "Import test 2", estimatedMinutes = 5 },
            TestJson.Options);
        var test = await created.Content.ReadFromJsonAsync<CatalogTestDetailDto>(TestJson.Options);

        var questions = new[]
        {
            new { code = "IQ2-1", order = 1, textUz = "Savol 1", type = "Likert5", scale = "STRESS", direction = 1, weight = 1.0m, isRequired = (bool?)null },
            new { code = "IQ2-2", order = 2, textUz = "Savol 2", type = "Likert5", scale = "STRESS", direction = 1, weight = 1.0m, isRequired = (bool?)null },
        };

        var response = await client.PostAsJsonAsync($"/api/admin/catalog/tests/{test!.Id}/questions/import", new { questions }, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CatalogTestDetailDto>(TestJson.Options);
        body!.Status.Should().Be("Draft");
        body.QuestionCount.Should().Be(2);
    }

    /// <summary>
    /// "ENG MUHIM" #2: nashr qilingandan keyin (kesh to'ldirilgach) test metadatasi
    /// yangilansa (`PUT /tests/{id}`) ommaviy kesh yozuvi DARHOL o'chirilishi kerak —
    /// aks holda o'quvchi TTL (10 daqiqa) tugagunga qadar eski `pageSize`/`Version`ni ko'rar edi.
    /// </summary>
    [Fact]
    public async Task UpdateTest_NashrQilinganTestKeshdaBolganda_KeshniBekorQiladi()
    {
        using var client = await AuthenticatedClientAsync("catalog-cache-invalidate-admin");

        Guid testId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;
            var testId2 = Guid.NewGuid();
            var question = Question.Create(Guid.NewGuid(), testId2, "CACHE-Q1", 1, "Savol", QuestionType.Likert5, "GEN", 1, 1.0m);
            var test = TestDefinition.Create(testId2, "CACHE-TEST-1", "Kesh testi", 1, 5, scoringStrategyCode: "SUM", now: now, kind: TestKind.Custom, isSystem: false);
            test.AddQuestion(question, now);
            test.Publish(now);
            db.TestDefinitions.Add(test);
            await db.SaveChangesAsync();
            testId = testId2;
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var executor = scope.ServiceProvider.GetRequiredService<IAsyncQueryExecutor>();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var catalogCache = scope.ServiceProvider.GetRequiredService<PublicCatalogCache>();

            // Keshni to'ldiramiz (o'quvchi bir marta so'raganidek).
            var cached = await catalogCache.GetPublishedTestDefinitionAsync("CACHE-TEST-1", CancellationToken.None);
            cached.Should().NotBeNull();

            cache.TryGet<CachedTestDefinitionDto>(PublicCatalogCache.TestDefinitionCacheKey("CACHE-TEST-1"), out _).Should().BeTrue();
        }

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/admin/catalog/tests/{testId}",
            new { nameUz = "Kesh testi (yangilangan)", descriptionUz = (string?)null, displayOrder = 1, estimatedMinutes = 5, shuffleQuestions = false, pageSize = 15 },
            TestJson.Options);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
            cache.TryGet<CachedTestDefinitionDto>(PublicCatalogCache.TestDefinitionCacheKey("CACHE-TEST-1"), out _)
                .Should().BeFalse("PUT /tests/{id} pageSize'ni o'zgartirgani uchun ommaviy kesh bekor qilinishi shart");
        }
    }
}
