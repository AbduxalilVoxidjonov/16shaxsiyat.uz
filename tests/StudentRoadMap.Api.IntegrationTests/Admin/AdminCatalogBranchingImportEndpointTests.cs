using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Application.Public.GetTestQuestions;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Catalog.Branching;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// P52 (`docs/18` §7) — **eng muhim qabul mezoni**: `docs/examples/sorovnoma-intellect.json`
/// admin API orqali (test yaratish → bo'limlar → savollarni import qilish → nashr) muvaffaqiyatli
/// import bo'lishi va nashr qilinishi, so'ng ommaviy `GET .../questions` 5 bo'lim va 25 savol
/// qaytarishi. Alohida sinf/faylda — bitta og'ir integratsion oqim, login kvotasi bilan
/// bog'liq emas (`AdminCatalogSectionsEndpointTests`dagi izohga qarang).
/// </summary>
public sealed class AdminCatalogBranchingImportEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminCatalogBranchingImportEndpointTests(PublicApiTestFactory factory)
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

    private sealed record ImportedOption(string TextUz, int Value, int DisplayOrder);

    private sealed record ImportedQuestion(
        string Code, int Order, string? SectionCode, string TextUz, string Type, string Scale, int Direction, decimal Weight,
        bool? IsRequired, string? Placeholder, string? InputPattern, int? MaxLength, int? MinSelections, int? MaxSelections,
        VisibilityRule? Visibility, IReadOnlyList<ImportedOption>? Options);

    private sealed record ImportedSection(string Code, string TitleUz, string? DescriptionUz, int DisplayOrder, VisibilityRule? Visibility);

    private sealed record ImportedTestFile(
        string Code, string NameUz, string? DescriptionUz, int EstimatedMinutes, int? PageSize, bool? ShuffleQuestions,
        int? DisplayOrder, string? ScoringMode, IReadOnlyList<ImportedSection> Sections, IReadOnlyList<ImportedQuestion> Questions);

    private static readonly JsonSerializerOptions FixtureJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task SorovnomaIntellectJson_ImportPublishVaOmmaviyApi_5BolimVa25SavolQaytaradi()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "examples", "sorovnoma-intellect.json");
        File.Exists(fixturePath).Should().BeTrue("`docs/examples/sorovnoma-intellect.json` chiqish katalogiga nusxalanishi kerak (csproj `Content`)");

        var fixture = JsonSerializer.Deserialize<ImportedTestFile>(File.ReadAllText(fixturePath), FixtureJsonOptions);
        fixture.Should().NotBeNull();
        fixture!.Sections.Should().HaveCount(5);
        fixture.Questions.Should().HaveCount(25);

        using var client = await AuthenticatedClientAsync("catalog-intellect-import-admin");

        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/catalog/tests",
            new
            {
                code = fixture.Code,
                nameUz = fixture.NameUz,
                descriptionUz = fixture.DescriptionUz,
                estimatedMinutes = fixture.EstimatedMinutes,
                pageSize = fixture.PageSize,
                shuffleQuestions = fixture.ShuffleQuestions,
                displayOrder = fixture.DisplayOrder,
                scoringMode = fixture.ScoringMode,
            },
            TestJson.Options);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, await createResponse.Content.ReadAsStringAsync());
        var test = await createResponse.Content.ReadFromJsonAsync<CatalogTestDetailDto>(TestJson.Options);

        foreach (var section in fixture.Sections)
        {
            var sectionResponse = await client.PostAsJsonAsync(
                $"/api/admin/catalog/tests/{test!.Id}/sections",
                new { code = section.Code, titleUz = section.TitleUz, descriptionUz = section.DescriptionUz, displayOrder = section.DisplayOrder, visibility = section.Visibility },
                TestJson.Options);
            sectionResponse.StatusCode.Should().Be(HttpStatusCode.Created, $"'{section.Code}' bo'limi import bo'lishi kerak: {await sectionResponse.Content.ReadAsStringAsync()}");
        }

        var importResponse = await client.PostAsJsonAsync(
            $"/api/admin/catalog/tests/{test!.Id}/questions/import",
            new { questions = fixture.Questions },
            TestJson.Options);
        importResponse.StatusCode.Should().Be(HttpStatusCode.OK, await importResponse.Content.ReadAsStringAsync());
        var afterImport = await importResponse.Content.ReadFromJsonAsync<CatalogTestDetailDto>(TestJson.Options);
        afterImport!.QuestionCount.Should().Be(25);

        var publishResponse = await client.PostAsync(new Uri($"/api/admin/catalog/tests/{test.Id}/publish", UriKind.Relative), content: null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK, await publishResponse.Content.ReadAsStringAsync());

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataFactory.AttachTestToDefaultProgramAsync(db, DateTimeOffset.UtcNow, test.Id, displayOrder: 1);
        }

        var accessToken = TestDataFactory.NewAccessToken("intellect-import");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataFactory.CreateSchoolAsync(db, DateTimeOffset.UtcNow, "maktab-intellect-import", accessToken);
        }

        using var publicClient = _factory.CreateClient();
        var sessionCommand = new StartSessionCommand(
            "maktab-intellect-import", accessToken, null, "Test O'quvchi Import", new DateOnly(2011, 3, 3),
            Gender.Female, 8, "B", "+998901234567", null, null, true, "uz");
        var sessionResponse = await publicClient.PostAsJsonAsync("/api/public/sessions", sessionCommand, TestJson.Options);
        sessionResponse.StatusCode.Should().Be(HttpStatusCode.Created, await sessionResponse.Content.ReadAsStringAsync());
        var session = await sessionResponse.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);

        publicClient.DefaultRequestHeaders.Add("X-Session-Token", session!.SessionToken);
        var startTestResponse = await publicClient.PostAsync(new Uri($"/api/public/sessions/tests/{fixture.Code}/start", UriKind.Relative), content: null);
        startTestResponse.StatusCode.Should().Be(HttpStatusCode.OK, await startTestResponse.Content.ReadAsStringAsync());

        var questionsResponse = await publicClient.GetAsync(new Uri($"/api/public/sessions/tests/{fixture.Code}/questions?page=1", UriKind.Relative));
        questionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var questionsBody = await questionsResponse.Content.ReadFromJsonAsync<GetTestQuestionsResult>(TestJson.Options);

        questionsBody!.Sections.Should().HaveCount(5, "docs/18 §7: 5 bo'lim");
        questionsBody.Questions.Should().HaveCount(25, "docs/18 §7: 25 savol");
        questionsBody.TotalQuestions.Should().Be(25);
        questionsBody.Page.Should().Be(1);
        questionsBody.TotalPages.Should().Be(1, "bo'limli anketada sahifalash o'chadi (docs/18 §4.1)");
    }
}
