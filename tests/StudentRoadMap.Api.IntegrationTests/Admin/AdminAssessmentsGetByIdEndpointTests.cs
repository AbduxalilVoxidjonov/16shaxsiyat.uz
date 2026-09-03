using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Assessments;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `AssessmentsController.GetById` — `docs/07` 3.3-bo'lim. Javob `AdminAssessmentDetailDto`:
/// `latestAssessment` yadrosi (`{id, results, aiAnalysis, aiHistory}`) + sessiya sarlavhasi
/// (holat, vaqtlar, ishonchlilik, o'quvchi, maktab, dastur) + `tests[]` (2026-09-03).
///
/// <para>
/// **Nega XOM JSON tekshiriladi:** `ReadFromJsonAsync&lt;Dto&gt;` kalit nomidagi farqni KO'RMAYDI
/// (deserializer kalitni o'zi qayta bog'laydi) — ilgari aynan shu sabab `results` kalitlaridagi
/// ikkita xato omon qolgan edi (2026-09-03 qarori, `AdminTestResultsJsonKeysTests`). Shu sabab
/// har yangi maydon shu yerda `JsonDocument` bilan HARFMA-HARF tekshiriladi.
/// </para>
/// Alohida `IClassFixture`.
/// </summary>
public sealed class AdminAssessmentsGetByIdEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminAssessmentsGetByIdEndpointTests(PublicApiTestFactory factory)
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
    /// `Survey` (BALLANMAYDIGAN) anketa — `docs/06` 8-bo'lim, 2026-09-02 qarori. `TestDataFactory`
    /// bunday anketa yaratishni bilmaydi (u faqat `Scored`), shu sabab shu yerda joyida quriladi;
    /// dasturga biriktirilmaydi — detal endpointi `tests[]`ni SESSIYA bloklari (`assessment_tests`)
    /// dan oladi, dasturdan emas.
    /// </summary>
    private static async Task<TestDefinition> CreatePublishedSurveyTestAsync(AppDbContext db, DateTimeOffset now, string code, int questionCount)
    {
        var testId = Guid.NewGuid();
        var test = TestDefinition.Create(
            testId,
            code,
            $"{code} so'rovnomasi",
            displayOrder: 9,
            estimatedMinutes: 3,
            scoringStrategyCode: null,
            now: now,
            scoringMode: TestScoringMode.Survey);

        for (var i = 1; i <= questionCount; i++)
        {
            test.AddQuestion(
                Question.Create(
                    Guid.NewGuid(),
                    testId,
                    $"{code}-Q{i:00}",
                    i,
                    $"{code} savoli {i}",
                    QuestionType.Likert5,
                    "GEN",
                    scaleDirection: 1,
                    weight: 1.0m,
                    isRequired: true),
                now);
        }

        test.Publish(now);
        db.TestDefinitions.Add(test);
        await db.SaveChangesAsync();

        return test;
    }

    [Fact]
    public async Task GetById_MavjudEmas_404Qaytaradi()
    {
        using var client = await AuthenticatedClientAsync("assessments-getbyid-404-admin");

        var response = await client.GetAsync(new Uri($"/api/admin/assessments/{Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_YakunlanganSessiya_NatijaVaScaleMaydoniYoqliginiQaytaradi()
    {
        using (var seedScope = _factory.Services.CreateScope())
        {
            var seeder = seedScope.ServiceProvider.GetRequiredService<StudentRoadMap.Infrastructure.Persistence.Seeding.DbSeeder>();
            await seeder.SeedAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "assess-getbyid-a", TestDataFactory.NewAccessToken("assess-getbyid-a"));
        // ⚠️ HAQIQIY batareya anketasi (`Standard` + `Scored` + `MBTI16` STRATEGIYASI), lekin
        // kodi ATAYLAB `MBTI16` EMAS — `GBA-MBTI`. `results.MBTI16` bloki 2026-09-03 dan buyon
        // metodika KODI emas, batareya ROLI bo'yicha to'ldiriladi (`StudentProfileMapping`).
        var mbtiTest = await TestDataFactory.CreateStandaloneSystemTestAsync(
            db, now, "GBA-MBTI", 1, questionCount: 1, scoringStrategyCode: "MBTI16");

        var phone = PhoneNumber.Create("+998907772001").Value;
        var student = Student.Create(Guid.NewGuid(), school.Id, "Nortoyev Aziz Shukurovich", new DateOnly(2009, 6, 12), Gender.Male, 8, phone, now, now);
        db.Students.Add(student);

        const string sessionToken = "assess-getbyid-session-token-0123456789ab";
        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var program = await db.AssessmentPrograms.AsNoTracking().FirstAsync(p => p.Id == programId);
        var assessment = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, sessionToken, "uz", programId, startedAt: now.AddMinutes(-30), expiresAt: now.AddDays(7), now: now);
        var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, mbtiTest.Id, 1, totalCount: 1);
        assessment.AddTest(assessmentTest);

        var questionId = (await db.Questions.AsNoTracking().FirstAsync(q => q.TestDefinitionId == mbtiTest.Id)).Id;
        assessment.StartTest(mbtiTest.Id, now);
        assessmentTest.UpsertAnswer(Guid.NewGuid(), questionId, 4, null, 2000, now);
        assessment.CompleteTest(mbtiTest.Id, [questionId], now);
        assessment.Complete(now);
        assessment.SetReliability(85.5, ReliabilityFlag.Reliable, now);

        var normalizedScores = """{"EI":28.3,"SN":71.6,"TF":33.3,"JP":64.1}""";
        var levels = """{"EI":"I","SN":"N","TF":"T","JP":"J"}""";
        var testResult = TestResult.Create(
            Guid.NewGuid(), assessmentTest.Id, assessment.Id, mbtiTest.Code, "{}", normalizedScores,
            scoringVersion: 1, testVersion: 1, computedAt: now, resultCode: "INTJ", levelsJson: levels, flagsJson: "[]");

        db.Assessments.Add(assessment);
        db.TestResults.Add(testResult);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("assessments-getbyid-admin");

        var response = await client.GetAsync(new Uri($"/api/admin/assessments/{assessment.Id}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = (await response.Content.ReadFromJsonAsync<AdminAssessmentDetailDto>(TestJson.Options))!;
        body.Id.Should().Be(assessment.Id);
        body.Results.Mbti16.Should().NotBeNull();
        body.Results.Mbti16!.ResultCode.Should().Be("INTJ");
        body.Results.Mbti16.TypeName.Should().Be("Loyihachi");
        body.Results.Big5.Should().BeNull();
        body.AiAnalysis.Should().BeNull("P16-P18 (AI modul) hali ulanmagan");
        body.AiHistory.Should().BeEmpty();

        // Sarlavha ma'lumoti — endi javobning O'ZIDA (navigatsiya holatiga tayanmaydi).
        body.Status.Should().Be("Completed");
        body.StartedAt.Should().BeCloseTo(now.AddMinutes(-30), TimeSpan.FromSeconds(5));
        body.CompletedAt.Should().NotBeNull();
        body.DurationMinutes.Should().Be(30);
        body.ReliabilityScore.Should().Be(85.5);
        body.ReliabilityFlag.Should().Be("Reliable");
        body.Student!.Id.Should().Be(student.Id);
        body.Student.FullName.Should().Be(student.FullName);
        body.School!.Id.Should().Be(school.Id);
        body.School.Name.Should().Be(school.Name);
        body.Program!.Id.Should().Be(programId);
        body.Program.NameUz.Should().Be(program.NameUz);
        body.Tests.Should().ContainSingle(t => t.TestCode == "GBA-MBTI");

        // `CLAUDE.md` 9-band: `scale`/`scaleDirection` javobda HECH QACHON bo'lmaydi.
        var raw = await client.GetStringAsync(new Uri($"/api/admin/assessments/{assessment.Id}", UriKind.Relative));
        raw.Should().NotContain("scaleDirection");

        // XOM JSON: kalit nomlari shartnoma (`ReadFromJsonAsync` farqni ko'rmaydi).
        using var document = JsonDocument.Parse(raw);
        var root = document.RootElement;

        root.GetProperty("status").GetString().Should().Be("Completed");
        root.GetProperty("completedAt").ValueKind.Should().Be(JsonValueKind.String);
        root.GetProperty("durationMinutes").GetInt32().Should().Be(30);
        root.GetProperty("reliabilityScore").GetDouble().Should().Be(85.5);
        root.GetProperty("reliabilityFlag").GetString().Should().Be("Reliable");
        root.GetProperty("student").GetProperty("id").GetGuid().Should().Be(student.Id);
        root.GetProperty("student").GetProperty("fullName").GetString().Should().Be(student.FullName);
        root.GetProperty("school").GetProperty("id").GetGuid().Should().Be(school.Id);
        root.GetProperty("school").GetProperty("name").GetString().Should().Be(school.Name);
        root.GetProperty("program").GetProperty("id").GetGuid().Should().Be(programId);
        root.GetProperty("program").GetProperty("nameUz").GetString().Should().Be(program.NameUz);

        // `results` kalitlari KATTA harfda qoladi (2026-09-03 qarori) — yangi maydonlar buni buzmaydi.
        var results = root.GetProperty("results");
        results.GetProperty("MBTI16").GetProperty("resultCode").GetString().Should().Be("INTJ");
        results.TryGetProperty("mbti16", out _).Should().BeFalse("kalitlar `TestDefinition.Code` bilan harfma-harf bir xil");

        var testItem = root.GetProperty("tests").EnumerateArray().Single();
        testItem.GetProperty("testDefinitionId").GetGuid().Should().Be(mbtiTest.Id);
        testItem.GetProperty("testCode").GetString().Should().Be("GBA-MBTI");
        testItem.GetProperty("nameUz").GetString().Should().Be(mbtiTest.NameUz);
        testItem.GetProperty("scoringMode").GetString().Should().Be("Scored");
        testItem.GetProperty("status").GetString().Should().Be("Completed");
        testItem.GetProperty("questionCount").GetInt32().Should().Be(1);
        testItem.GetProperty("answeredCount").GetInt32().Should().Be(1);
    }

    /// <summary>
    /// Yakunlanmagan sessiya: `completedAt`/`durationMinutes`/`reliabilityScore`/`reliabilityFlag`
    /// — XOM JSON'da `null` (`0` yoki bo'sh satr EMAS, `docs/06` qarorlar jurnali, 2026-09-02).
    /// Shu bilan birga `tests[]` dasturdagi BALLANMAYDIGAN (`Survey`) anketani ham ko'rsatadi —
    /// u `results`da hech qachon paydo bo'lmaydi, shuning uchun admin uni faqat shu ro'yxatdan ko'radi.
    /// </summary>
    [Fact]
    public async Task GetById_YakunlanmaganSessiya_NullQaytaradiVaSurveyAnketaniRoYxatlaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "assess-getbyid-b", TestDataFactory.NewAccessToken("assess-getbyid-b"));
        var scoredTest = await TestDataFactory.CreatePublishedTestAsync(db, now, "GBB-SCORED", 1, questionCount: 2);
        var surveyTest = await CreatePublishedSurveyTestAsync(db, now, "GBB-SURVEY", questionCount: 3);

        var phone = PhoneNumber.Create("+998907772002").Value;
        var student = Student.Create(Guid.NewGuid(), school.Id, "Qodirova Malika Anvarovna", new DateOnly(2010, 2, 3), Gender.Female, 9, phone, now, now);
        db.Students.Add(student);

        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var assessment = Assessment.Create(
            Guid.NewGuid(), student.Id, school.Id, "assess-getbyid-open-token-0123456789ab", "uz", programId,
            startedAt: now.AddMinutes(-10), expiresAt: now.AddDays(7), now: now);

        var scoredBlock = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, scoredTest.Id, 1, totalCount: 2);
        var surveyBlock = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, surveyTest.Id, 2, totalCount: 3);
        assessment.AddTest(scoredBlock);
        assessment.AddTest(surveyBlock);

        var firstQuestionId = (await db.Questions.AsNoTracking()
            .Where(q => q.TestDefinitionId == scoredTest.Id)
            .OrderBy(q => q.DisplayOrder)
            .FirstAsync()).Id;
        assessment.StartTest(scoredTest.Id, now);
        scoredBlock.UpsertAnswer(Guid.NewGuid(), firstQuestionId, 3, null, 1500, now);

        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("assessments-getbyid-open-admin");

        var raw = await client.GetStringAsync(new Uri($"/api/admin/assessments/{assessment.Id}", UriKind.Relative));
        using var document = JsonDocument.Parse(raw);
        var root = document.RootElement;

        root.GetProperty("status").GetString().Should().Be("InProgress");
        root.GetProperty("completedAt").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("durationMinutes").ValueKind.Should().Be(JsonValueKind.Null, "davomiylik hisoblanmagan — `0` EMAS");
        root.GetProperty("reliabilityScore").ValueKind.Should().Be(JsonValueKind.Null, "ishonchlilik hisoblanmagan — `0` EMAS");
        root.GetProperty("reliabilityFlag").ValueKind.Should().Be(JsonValueKind.Null);

        var tests = root.GetProperty("tests").EnumerateArray().ToList();
        tests.Should().HaveCount(2);

        // Tartib — `AssessmentTest.DisplayOrder` bo'yicha.
        tests[0].GetProperty("testCode").GetString().Should().Be("GBB-SCORED");
        tests[0].GetProperty("scoringMode").GetString().Should().Be("Scored");
        tests[0].GetProperty("status").GetString().Should().Be("InProgress");
        tests[0].GetProperty("questionCount").GetInt32().Should().Be(2);
        tests[0].GetProperty("answeredCount").GetInt32().Should().Be(1);

        tests[1].GetProperty("testCode").GetString().Should().Be("GBB-SURVEY");
        tests[1].GetProperty("nameUz").GetString().Should().Be(surveyTest.NameUz);
        tests[1].GetProperty("scoringMode").GetString().Should().Be("Survey", "so'rovnoma BALLANMAYDI — UI uni `0` ball deb ko'rsatmasligi uchun");
        tests[1].GetProperty("status").GetString().Should().Be("NotStarted");
        tests[1].GetProperty("answeredCount").GetInt32().Should().Be(0);
    }
}
