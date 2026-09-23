using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Persistence.Seeding;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// `GET /api/public/sessions/result` — `docs/07` 1.9-bo'lim, `prompts/12`. `App:ShowResultToStudent`
/// yoqilgan holat (`ShowResultApiTestFactory`) — o'chirilgan (standart) holat alohida
/// class'da, standart `PublicApiTestFactory` bilan (pastga qarang).
/// </summary>
public sealed class PublicGetStudentResultEndpointTests : IClassFixture<ShowResultApiTestFactory>
{
    private readonly ShowResultApiTestFactory _factory;

    public PublicGetStudentResultEndpointTests(ShowResultApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetStudentResult_TahlilAnalyzedBolmagan_202AcceptedQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("result-notready1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-result-notready1", accessToken, showResultToStudent: true);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "RNR1", 1, questionCount: 2);

        using var client = _factory.CreateClient();
        // `programCode` ANIQ ko'rsatiladi (`TestDataFactory.DefaultProgramCode` izohiga qarang) —
        // shu sinfdagi BOSHQA fact (`GetStudentResult_AnalyzedHolatida_...`) haqiqiy
        // `DbSeeder.SeedAsync()`ni chaqiradi (`PERSONALITY_PROFILE` tizim dasturini yaratadi),
        // shared `IClassFixture` bazasida ikkalasi ham "mavjud dastur" bo'lib qolishi mumkin.
        var sessionToken = await StartSessionAsync(
            client, school, accessToken, "Tursunov Alisher Farrukovich", new DateOnly(2010, 5, 5), TestDataFactory.DefaultProgramCode);
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        // Sessiya hali `Draft` — tahlil (`Analyzed`) HAR DOIM bu bosqichda tayyor emas
        // (AI navbati P18'gacha ulanmagan).
        var response = await client.GetAsync(new Uri("/api/public/sessions/result", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task GetStudentResult_AnalyzedHolatida_QisqartirilganNatijaVaMaxfiyMaydonlarYoq()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        using (var seedScope = _factory.Services.CreateScope())
        {
            // `TypeCatalog`/`CareerMap` — `typeName`/`shortDescription`/`topStrengths`/`careerFields`
            // uchun kerak (`DbSeeder` `test-definitions`ni ham seed qiladi, lekin bu testda ular
            // ishlatilmaydi — faqat 2 ta minimal `TestDefinition` qo'lda yaratiladi, pastga qarang).
            var seeder = seedScope.ServiceProvider.GetRequiredService<DbSeeder>();
            await seeder.SeedAsync();
        }

        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("result-ready1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-result-ready1", accessToken, showResultToStudent: true);

        // Haqiqiy `MBTI16`/`RIASEC` `TestDefinition`lar `DbSeeder` orqali allaqachon mavjud —
        // ularning ID'lari `AssessmentTest.TestDefinitionId` uchun ishlatiladi. Scoring
        // ENGINE bu yerda chaqirilmaydi (bu `CompleteTest`ning o'zi allaqachon boshqa testda
        // sinalgan) — `TestResult` to'g'ridan-to'g'ri kutilgan `ResultCode` bilan yoziladi,
        // shu bilan `GetStudentResultQueryHandler`ning TypeCatalog/CareerMap moslashtirish
        // mantig'i izolyatsiya qilingan holda sinaladi.
        var mbtiDefinitionId = (await db.TestDefinitions.AsNoTracking().SingleAsync(t => t.Code == "MBTI16")).Id;
        var riasecDefinitionId = (await db.TestDefinitions.AsNoTracking().SingleAsync(t => t.Code == "RIASEC")).Id;
        var mbtiQuestionId = (await db.Questions.AsNoTracking().FirstAsync(q => q.TestDefinitionId == mbtiDefinitionId)).Id;
        var riasecQuestionId = (await db.Questions.AsNoTracking().FirstAsync(q => q.TestDefinitionId == riasecDefinitionId)).Id;

        var phone = PhoneNumber.Create("+998901234567").Value;
        var student = Student.Create(Guid.NewGuid(), school.Id, "Aliyev Sardor Bekzodovich", new DateOnly(2010, 4, 17), Gender.Male, 9, phone, now, now);
        db.Students.Add(student);

        const string sessionToken = "get-student-result-test-session-token-0123456789";
        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var assessment = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, sessionToken, "uz", programId, startedAt: now.AddMinutes(-20), expiresAt: now.AddDays(7), now: now);

        var mbtiTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, mbtiDefinitionId, 1, totalCount: 1);
        var riasecTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, riasecDefinitionId, 2, totalCount: 1);
        assessment.AddTest(mbtiTest);
        assessment.AddTest(riasecTest);

        assessment.StartTest(mbtiDefinitionId, now);
        mbtiTest.UpsertAnswer(Guid.NewGuid(), mbtiQuestionId, 4, null, 2000, now);
        assessment.CompleteTest(mbtiDefinitionId, [mbtiQuestionId], now);

        assessment.StartTest(riasecDefinitionId, now);
        riasecTest.UpsertAnswer(Guid.NewGuid(), riasecQuestionId, 4, null, 2000, now);
        assessment.CompleteTest(riasecDefinitionId, [riasecQuestionId], now);

        assessment.Complete(now);
        assessment.SetReliability(90.0, ReliabilityFlag.Reliable, now);
        assessment.MarkAnalyzing(now);
        assessment.MarkAnalyzed(now);

        // "INTJ" (type-catalog.json, docs/07 1.9 misoli bilan bir xil) va "IRA" (career-map.json
        // "IR" yozuviga mos — birinchi ikki harfi `I`/`R`) — ikkalasi ham haqiqiy seed'da mavjud.
        var mbtiResult = TestResult.Create(Guid.NewGuid(), mbtiTest.Id, assessment.Id, "MBTI16", "{}", "{}", scoringVersion: 1, testVersion: 1, computedAt: now, resultCode: "INTJ");
        var riasecResult = TestResult.Create(Guid.NewGuid(), riasecTest.Id, assessment.Id, "RIASEC", "{}", "{}", scoringVersion: 1, testVersion: 1, computedAt: now, resultCode: "IRA");

        db.Assessments.Add(assessment);
        db.TestResults.AddRange(mbtiResult, riasecResult);
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        var response = await client.GetAsync(new Uri("/api/public/sessions/result", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var raw = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(raw).RootElement;

        // `docs/07` 1.9-bo'lim misoli bilan mos hujjatlashtirilgan maydonlar.
        json.GetProperty("personalityType").GetString().Should().Be("INTJ");
        json.GetProperty("typeName").GetString().Should().Be("Loyihachi");
        json.GetProperty("shortDescription").GetString().Should().NotBeNullOrWhiteSpace();
        json.GetProperty("topStrengths").EnumerateArray().Should().NotBeEmpty();
        json.GetProperty("careerFields").EnumerateArray().Should().NotBeEmpty("RIASEC 'IRA' kodi seed'dagi 'IR' kasb yo'nalishi bilan mos kelishi kerak");
        json.GetProperty("note").GetString().Should().NotBeNullOrWhiteSpace();

        // `prompts/12` cheklovi + `CLAUDE.md` 9-qoida: bu javobda HECH QACHON bo'lmasligi
        // shart bo'lgan maxfiy/ichki maydonlar — kompilyatsiya darajasida ham (`GetStudentResultResult`
        // recordida bu xususiyatlar umuman yo'q), lekin haqiqiy tarmoq javobida ham tasdiqlanadi.
        var forbiddenKeys = new[]
        {
            "maturityIndex", "activityIndex", "reliabilityScore", "reliabilityFlag",
            "rawScores", "normalizedScores", "flags", "needsAttention",
            "scale", "scaleDirection",
        };

        foreach (var key in forbiddenKeys)
        {
            json.TryGetProperty(key, out _).Should().BeFalse($"'{key}' o'quvchi javobida bo'lmasligi shart");
        }
    }

    private static async Task<string> StartSessionAsync(HttpClient client, StudentRoadMap.Domain.Schools.School school, string accessToken, string fullName, DateOnly birthDate, string? programCode = null)
    {
        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, fullName, birthDate, Gender.Male, 9, "A",
            "+998901234567", "+998909998877", null, true, "uz", programCode);

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);
        return body!.SessionToken;
    }
}

/// <summary>
/// Natija ko'rsatish O'CHIQ holat — `403 FORBIDDEN`. P47dan buyon bu MAKON bayrog'i
/// (`School.ShowResultToStudent`, maktab uchun standart `false`) hisobiga: global bayroq
/// (`App:ShowResultToStudent`) endi standart `true` (kill-switch). Global rubilnikning
/// o'zi butun tizimni yopishi `PublicResultKillSwitchEndpointTests` da sinaladi.
/// </summary>
public sealed class PublicGetStudentResultForbiddenEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicGetStudentResultForbiddenEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetStudentResult_MakonBayrogiOchirilgan_403ForbiddenQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("result-forbidden1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-result-forbidden1", accessToken);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "RF1", 1, questionCount: 2);

        using var client = _factory.CreateClient();
        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, "Ismoilova Zarina Otabekovna", new DateOnly(2010, 6, 6), Gender.Female, 9, "A",
            "+998901234567", "+998909998877", null, true, "uz");
        var startResponse = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        startResponse.EnsureSuccessStatusCode();
        var sessionToken = (await startResponse.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!.SessionToken;
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        var response = await client.GetAsync(new Uri("/api/public/sessions/result", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("FORBIDDEN");
    }
}
