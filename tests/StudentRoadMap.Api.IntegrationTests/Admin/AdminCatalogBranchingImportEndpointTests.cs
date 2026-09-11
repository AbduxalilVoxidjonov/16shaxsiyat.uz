using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Application.Public.GetTestQuestions;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Application.Seeding;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// P52 (`docs/18` §7) → namunaviy so'rovnomani SEED qilish topshirig'i — **eng muhim qabul
/// mezoni**: `Infrastructure/Persistence/SeedData/surveys/intellect-survey.json` (aynan
/// `DbSeeder.SeedSurveysAsync` ISHLATADIGAN o'sha yo'l — `SeedDataLoader.ParseSurvey` +
/// `ToDomainCustomDraftSurvey`) bilan qurilgan anketa admin tomonidan nashr qilinishi va
/// ommaviy `GET .../questions` 5 bo'lim/25 savol qaytarishi kerak.
///
/// ⚠️ Ilgari (P52, birinchi versiya) bu test fixture'ni HTTP import zanjiri orqali
/// (`POST .../tests` → `.../sections` → `.../questions/import`) qurar edi. Namuna endi SEED
/// bo'lgani sabab bu zanjir haqiqiy production oqimida HECH QACHON ishlatilmaydi (anketa
/// allaqachon `--seed` bosqichida tayyor keladi) — shu sabab bu yerda TO'G'RIDAN-TO'G'RI
/// `SeedDataLoader` chaqiriladi (seed yo'liga moslashtirilgan, A5'ning eski import-orqali
/// tekshiruvi bilan takrorlanmaydi) va faqat NASHR → DASTURGA BIRIKTIRISH → OMMAVIY API
/// oqimi HTTP orqali sinaladi. Alohida sinf/faylda qoladi — bitta og'ir integratsion oqim,
/// login kvotasi bilan bog'liq emas (`AdminCatalogSectionsEndpointTests`dagi izohga qarang).
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

    [Fact]
    public async Task SeedQilinganSorovnoma_PublishVaOmmaviyApi_5BolimVa25SavolQaytaradi()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "SeedData", "surveys", "intellect-survey.json");
        File.Exists(fixturePath).Should().BeTrue("`Infrastructure/Persistence/SeedData/surveys/intellect-survey.json` chiqish katalogiga nusxalanishi kerak (csproj `Content`, `docs/18` §7)");

        Guid testDefinitionId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // `DbSeeder.SeedSurveysAsync` bilan BIR XIL yo'l — bu test seed pipeline'ining
            // o'zini emas (u `DbSeederTests`da SQLite ustida sinaladi), balki natijaviy
            // grafning nashr/ommaviy API bilan to'g'ri ishlashini tekshiradi.
            var json = await File.ReadAllTextAsync(fixturePath);
            var dto = SeedDataLoader.ParseSurvey(json, "intellect-survey.json");
            var testDefinition = SeedDataLoader.ToDomainCustomDraftSurvey(
                dto, Guid.NewGuid(), _ => Guid.NewGuid(), _ => Guid.NewGuid(), DateTimeOffset.UtcNow);

            testDefinition.Status.Should().Be(TestDefinitionStatus.Draft, "docs/18 §7: namunaviy so'rovnoma har doim Draft seed qilinadi");
            testDefinition.IsSystem.Should().BeFalse();
            testDefinition.Sections.Should().HaveCount(5);
            testDefinition.QuestionCount.Should().Be(25);

            db.TestDefinitions.Add(testDefinition);
            await db.SaveChangesAsync();
            testDefinitionId = testDefinition.Id;
        }

        using var client = await AuthenticatedClientAsync("catalog-intellect-seed-publish-admin");

        var publishResponse = await client.PostAsync(new Uri($"/api/admin/catalog/tests/{testDefinitionId}/publish", UriKind.Relative), content: null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK, await publishResponse.Content.ReadAsStringAsync());

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataFactory.AttachTestToDefaultProgramAsync(db, DateTimeOffset.UtcNow, testDefinitionId, displayOrder: 1);
        }

        var accessToken = TestDataFactory.NewAccessToken("intellect-seed");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataFactory.CreateSchoolAsync(db, DateTimeOffset.UtcNow, "maktab-intellect-seed", accessToken);
        }

        using var publicClient = _factory.CreateClient();
        var sessionCommand = new StartSessionCommand(
            "maktab-intellect-seed", accessToken, null, "Test O'quvchi Seed", new DateOnly(2011, 3, 3),
            Gender.Female, 8, "B", "+998901234567", null, null, true, "uz");
        var sessionResponse = await publicClient.PostAsJsonAsync("/api/public/sessions", sessionCommand, TestJson.Options);
        sessionResponse.StatusCode.Should().Be(HttpStatusCode.Created, await sessionResponse.Content.ReadAsStringAsync());
        var session = await sessionResponse.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);

        publicClient.DefaultRequestHeaders.Add("X-Session-Token", session!.SessionToken);
        var startTestResponse = await publicClient.PostAsync(new Uri("/api/public/sessions/tests/INTELLECT-SURVEY/start", UriKind.Relative), content: null);
        startTestResponse.StatusCode.Should().Be(HttpStatusCode.OK, await startTestResponse.Content.ReadAsStringAsync());

        var questionsResponse = await publicClient.GetAsync(new Uri("/api/public/sessions/tests/INTELLECT-SURVEY/questions?page=1", UriKind.Relative));
        questionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var questionsBody = await questionsResponse.Content.ReadFromJsonAsync<GetTestQuestionsResult>(TestJson.Options);

        questionsBody!.Sections.Should().HaveCount(5, "docs/18 §7: 5 bo'lim");
        questionsBody.Questions.Should().HaveCount(25, "docs/18 §7: 25 savol");
        questionsBody.TotalQuestions.Should().Be(25);
        questionsBody.Page.Should().Be(1);
        questionsBody.TotalPages.Should().Be(1, "bo'limli anketada sahifalash o'chadi (docs/18 §4.1)");
    }
}
