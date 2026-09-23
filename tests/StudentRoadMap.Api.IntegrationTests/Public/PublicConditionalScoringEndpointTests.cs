using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.CompleteSession;
using StudentRoadMap.Application.Public.CompleteTest;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// ⚠️ Shartli scoring — `docs/06` 8-bo'lim (2026-09-02 qaror), `prompts/34` D-band ("ENG NOZIK
/// #2"): `MaturityIndex`/`ActivityIndex` mos ma'lumot yo'q bo'lganda `null` qoladi (`0` emas),
/// `Survey` javoblari ballanmaydi va ishonchlilik hisobiga kirmaydi, `StudentSnapshot` eski
/// qiymatni o'chirmaydi. Har bir stsenariy ALOHIDA `IClassFixture` (`CLAUDE.md` talabi).
/// </summary>
public sealed class PublicSurveyOnlySessionEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicSurveyOnlySessionEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Faqat `Survey` testidan iborat dastur: sessiya xatosiz yakunlanadi, `MaturityIndex`/
    /// `ActivityIndex` `null` qoladi, `TestResult` ball YOZILMAYDI, yangi o'quvchida snapshot
    /// bo'sh (`null`) qolaveradi (o'chiriladigan eski qiymat yo'q).
    /// </summary>
    [Fact]
    public async Task CompleteSession_FaqatSurveyDasturi_XatosizYakunlanadiVaIndexlarNullQoladi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("survey-only1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-survey-only1", accessToken);

        var surveyTest = await TestDataFactory.CreateStandaloneTestAsync(
            db, now, "SURVEYONLY1", 1, questionCount: 3, scoringMode: TestScoringMode.Survey, scoringStrategyCode: null);
        await TestDataFactory.CreateProgramAsync(db, now, "SURVEY-ONLY-PROG1", [(surveyTest.Id, 1)]);

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Faqat Survey Talabasi", new DateOnly(2010, 5, 5));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        var startResponse = await client.PostAsync(new Uri("/api/public/sessions/tests/SURVEYONLY1/start", UriKind.Relative), content: null);
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var questionIds = await db.Questions.AsNoTracking()
            .Where(q => q.TestDefinitionId == surveyTest.Id)
            .OrderBy(q => q.DisplayOrder)
            .Select(q => q.Id)
            .ToListAsync();

        var payload = new { answers = questionIds.Select(id => new { questionId = id, value = 3, durationMs = 3000 }).ToList() };
        var saveResponse = await client.PostAsJsonAsync("/api/public/sessions/tests/SURVEYONLY1/answers", payload, TestJson.Options);
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var completeTestResponse = await client.PostAsync(new Uri("/api/public/sessions/tests/SURVEYONLY1/complete", UriKind.Relative), content: null);
        completeTestResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var completeTestBody = await completeTestResponse.Content.ReadFromJsonAsync<CompleteTestResult>(TestJson.Options);
        completeTestBody!.AllTestsCompleted.Should().BeTrue();

        var completeSessionResponse = await client.PostAsync(new Uri("/api/public/sessions/complete", UriKind.Relative), content: null);

        completeSessionResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Survey-only sessiya xato bermasdan yakunlanishi shart");

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await verifyDb.Assessments.AsNoTracking().SingleAsync(a => a.SessionToken == sessionToken);

        // `Ai:AutoAnalyzeOnCompletion` standart `false` — yakunlangan sessiya `Completed`da qoladi
        // (`docs/06` 8-bo'lim, 2026-09-03 egasi qarori). Yakunlash mantiqi o'zgarmagan.
        assessment.Status.Should().Be(AssessmentStatus.Completed);
        // Ishonchlilik hali ham hisoblanadi (u har qanday javob to'plamiga tegishli, `prompts/34` D12-band).
        assessment.ReliabilityScore.Should().NotBeNull();
        assessment.ReliabilityFlag.Should().NotBeNull();

        // `Survey` testda `TestResult` ball YOZILMAYDI (`docs/06` 8-bo'lim).
        var testResults = await verifyDb.TestResults.AsNoTracking().Where(r => r.AssessmentId == assessment.Id).ToListAsync();
        testResults.Should().BeEmpty("Survey testda ball yozilmaydi");

        var student = await verifyDb.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);
        student.LastMaturityIndex.Should().BeNull("BIG5/ACTIVITY yo'q — 0 emas, null");
        student.LastActivityIndex.Should().BeNull();
        student.LastPersonalityType.Should().BeNull();
        student.LastHollandCode.Should().BeNull();
    }

    private static async Task<string> StartSessionAsync(HttpClient client, School school, string accessToken, string fullName, DateOnly birthDate, string? programCode = null)
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
/// Faqat "BIG5" kodli test bo'lgan dasturda `MaturityIndex` `null` qoladi (`ACTIVITY` yo'q —
/// `CompleteSessionCommandHandler.ApplyMaturityIndexIfPossible` ikkalasi ham talab qiladi).
/// 2026-09-03 dan keyin bu yerda IKKI sabab bir vaqtda ishlaydi: anketa `Custom` (batareya roli
/// yo'q, `PersonalityBattery.RoleOf`) va sessiyada `Activity` rolidagi natija ham yo'q.
/// </summary>
public sealed class PublicBig5WithoutActivityEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicBig5WithoutActivityEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CompleteSession_FaqatBig5Dasturi_MaturityIndexNullQoladi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("big5-only1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-big5-only1", accessToken);

        // ⚠️ Anketa kodi `BIG5`, lekin u `Custom` — ya'ni `PersonalityBattery.RoleOf` bo'yicha
        // batareya roli YO'Q (2026-09-03 tuzatishi; ilgari `TestCode == "BIG5"` satri bilan
        // topilardi). Haqiqiy shkala tarkibi (bu yerda `RIASEC` shaklidagi, `SUM` emas —
        // `InterpretationBands` talab qilmaydi) ahamiyatsiz: `MaturityIndex` baribir
        // hisoblanmaydi (`ACTIVITY` roli ham yo'q).
        var big5Test = await TestDataFactory.CreateStandaloneRiasecShapedTestAsync(db, now, "BIG5", 1);
        await TestDataFactory.CreateProgramAsync(db, now, "BIG5-ONLY-PROG1", [(big5Test.Id, 1)]);

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "BIG5 Talabasi", new DateOnly(2010, 5, 5));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        await client.PostAsync(new Uri("/api/public/sessions/tests/BIG5/start", UriKind.Relative), content: null);

        var questionIds = await db.Questions.AsNoTracking()
            .Where(q => q.TestDefinitionId == big5Test.Id)
            .OrderBy(q => q.DisplayOrder)
            .Select(q => q.Id)
            .ToListAsync();

        var payload = new { answers = questionIds.Select(id => new { questionId = id, value = 3, durationMs = 3000 }).ToList() };
        await client.PostAsJsonAsync("/api/public/sessions/tests/BIG5/answers", payload, TestJson.Options);
        await client.PostAsync(new Uri("/api/public/sessions/tests/BIG5/complete", UriKind.Relative), content: null);

        var completeSessionResponse = await client.PostAsync(new Uri("/api/public/sessions/complete", UriKind.Relative), content: null);
        completeSessionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await verifyDb.Assessments.AsNoTracking().SingleAsync(a => a.SessionToken == sessionToken);
        var bigFiveResult = await verifyDb.TestResults.AsNoTracking().SingleAsync(r => r.AssessmentId == assessment.Id && r.TestCode == "BIG5");

        bigFiveResult.CompositeIndex.Should().BeNull("ACTIVITY bo'lmasa MaturityIndex hisoblanmaydi (0 emas, null)");

        var student = await verifyDb.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);
        student.LastMaturityIndex.Should().BeNull();
    }

    private static async Task<string> StartSessionAsync(HttpClient client, School school, string accessToken, string fullName, DateOnly birthDate, string? programCode = null)
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
/// `StudentSnapshot` ma'lumotsiz sessiya tufayli eski qiymatlarni o'CHIRMAYDI (`prompts/34`
/// D14-band, "ENG NOZIK #2"): o'quvchida oldindan haqiqiy `MaturityIndex`/`ActivityIndex`/tip
/// bor — keyingi (faqat `Survey`) sessiya bu qiymatlarni saqlab qoladi.
/// </summary>
public sealed class PublicStudentSnapshotPreservedEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicStudentSnapshotPreservedEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CompleteSession_MalumotsizIkkinchiSessiya_EskiSnapshotQiymatlariniOChirmaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("snapshot-preserve1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-snapshot-preserve1", accessToken);

        var surveyTest = await TestDataFactory.CreateStandaloneTestAsync(
            db, now, "SNAPSURVEY1", 1, questionCount: 2, scoringMode: TestScoringMode.Survey, scoringStrategyCode: null);
        await TestDataFactory.CreateProgramAsync(db, now, "SNAPSHOT-SURVEY-PROG1", [(surveyTest.Id, 1)]);

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Snapshot Talabasi", new DateOnly(2010, 5, 5));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        // O'quvchida OLDINDAN haqiqiy natijalar bor deb simulyatsiya qilamiz (avvalgi, to'liq
        // batareya bilan yakunlangan sessiyadan kelgan qiymatlar) — domen metodi orqali
        // to'g'ridan-to'g'ri o'rnatiladi (HTTP orqali haqiqiy BIG5/ACTIVITY oqimini qayta
        // qurish shart emas, faqat "eski qiymat saqlanib qoladimi" tekshiriladi).
        using (var setupScope = _factory.Services.CreateScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var assessment = await setupDb.Assessments.SingleAsync(a => a.SessionToken == sessionToken);
            var student = await setupDb.Students.SingleAsync(s => s.Id == assessment.StudentId);
            student.UpdateSnapshot(
                lastPersonalityType: "INTJ",
                lastMaturityIndex: 68.4,
                lastActivityIndex: 71.0,
                lastActivityLevel: ActivityLevel.Active,
                lastHollandCode: "IRA",
                needsAttention: false,
                lastAssessmentAt: now.AddDays(-30),
                completedAssessmentCount: 1,
                now: now.AddDays(-30));
            await setupDb.SaveChangesAsync();
        }

        await client.PostAsync(new Uri("/api/public/sessions/tests/SNAPSURVEY1/start", UriKind.Relative), content: null);

        var questionIds = await db.Questions.AsNoTracking()
            .Where(q => q.TestDefinitionId == surveyTest.Id)
            .OrderBy(q => q.DisplayOrder)
            .Select(q => q.Id)
            .ToListAsync();

        var payload = new { answers = questionIds.Select(id => new { questionId = id, value = 3, durationMs = 3000 }).ToList() };
        await client.PostAsJsonAsync("/api/public/sessions/tests/SNAPSURVEY1/answers", payload, TestJson.Options);
        await client.PostAsync(new Uri("/api/public/sessions/tests/SNAPSURVEY1/complete", UriKind.Relative), content: null);

        var completeSessionResponse = await client.PostAsync(new Uri("/api/public/sessions/complete", UriKind.Relative), content: null);
        completeSessionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var verifyAssessment = await verifyDb.Assessments.AsNoTracking().SingleAsync(a => a.SessionToken == sessionToken);
        var verifyStudent = await verifyDb.Students.AsNoTracking().SingleAsync(s => s.Id == verifyAssessment.StudentId);

        verifyStudent.LastMaturityIndex.Should().Be(68.4, "Survey-only sessiya eski MaturityIndex'ni o'chirmasligi shart");
        verifyStudent.LastActivityIndex.Should().Be(71.0);
        verifyStudent.LastPersonalityType.Should().Be("INTJ");
        verifyStudent.LastHollandCode.Should().Be("IRA");
        // `CompletedAssessmentCount` va `LastAssessmentAt` esa YANGILANADI (bu haqiqiy hodisa —
        // yangi sessiya chindan tugallandi), faqat BALL maydonlari saqlanadi.
        verifyStudent.CompletedAssessmentCount.Should().Be(2);
    }

    private static async Task<string> StartSessionAsync(HttpClient client, School school, string accessToken, string fullName, DateOnly birthDate, string? programCode = null)
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
/// `Survey` javoblari `ReliabilityCalculator` kirishiga KIRMAYDI (`prompts/34` D-band): bir xil
/// `Scored` test javoblariga qo'shimcha shubhali (bir xil qiymat, o'ta tez) `Survey` javoblari
/// qo'shilsa ham, `ReliabilityScore` FAQAT `Scored` javoblarga asoslangan sessiya bilan bir xil
/// chiqishi shart.
/// </summary>
public sealed class PublicSurveyExcludedFromReliabilityEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicSurveyExcludedFromReliabilityEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CompleteSession_ShubhaliSurveyJavoblariQoshilsaHam_ReliabilityScoreOzgarmaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        // --- Sessiya A: faqat `Scored` test (varlangan javoblar, normal davomiylik) ---
        var accessTokenA = TestDataFactory.NewAccessToken("rel-excl-a1");
        var schoolA = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-rel-excl-a1", accessTokenA);
        var scoredTestA = await TestDataFactory.CreateStandaloneRiasecShapedTestAsync(db, now, "RELSCOREDA1", 1);
        await TestDataFactory.CreateProgramAsync(db, now, "REL-PROG-A1", [(scoredTestA.Id, 1)]);

        var reliabilityScoreOnlyScored = await CompleteScoredOnlySessionAsync(
            db, schoolA, accessTokenA, scoredTestA.Id, "Faqat Scored Talabasi", "REL-PROG-A1");

        // --- Sessiya B: BIR XIL `Scored` javoblar + shubhali `Survey` test (bir xil qiymat, o'ta tez) ---
        var accessTokenB = TestDataFactory.NewAccessToken("rel-excl-b1");
        var schoolB = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-rel-excl-b1", accessTokenB);
        var scoredTestB = await TestDataFactory.CreateStandaloneRiasecShapedTestAsync(db, now, "RELSCOREDB1", 1);
        var surveyTestB = await TestDataFactory.CreateStandaloneTestAsync(
            db, now, "RELSURVEYB1", 2, questionCount: 10, scoringMode: TestScoringMode.Survey, scoringStrategyCode: null);
        await TestDataFactory.CreateProgramAsync(db, now, "REL-PROG-B1", [(scoredTestB.Id, 1), (surveyTestB.Id, 2)]);

        // Bu nuqtada School A/B ikkalasi ham IKKALA `Public` dasturni ko'radi (REL-PROG-A1 va
        // REL-PROG-B1) — `programCode` ANIQ ko'rsatiladi, aks holda `400 PROGRAM_REQUIRED` bo'lardi.
        var reliabilityScoreWithSurvey = await CompleteScoredPlusSuspiciousSurveySessionAsync(
            db, schoolB, accessTokenB, scoredTestB.Id, surveyTestB.Id, "Scored Plus Survey Talabasi", "REL-PROG-B1");

        reliabilityScoreWithSurvey.Should().Be(
            reliabilityScoreOnlyScored,
            "shubhali Survey javoblari qo'shilishi ishonchlilik ballini o'zgartirmasligi shart — ular hisobga umuman kirmaydi");
    }

    private async Task<double> CompleteScoredOnlySessionAsync(
        AppDbContext db, School school, string accessToken, Guid scoredTestId, string fullName, string programCode)
    {
        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, fullName, new DateOnly(2010, 5, 5), programCode);
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        var testCode = await db.TestDefinitions.AsNoTracking().Where(t => t.Id == scoredTestId).Select(t => t.Code).SingleAsync();
        await client.PostAsync(new Uri($"/api/public/sessions/tests/{testCode}/start", UriKind.Relative), content: null);

        var questionIds = await db.Questions.AsNoTracking()
            .Where(q => q.TestDefinitionId == scoredTestId)
            .OrderBy(q => q.DisplayOrder)
            .Select(q => q.Id)
            .ToListAsync();

        var varied = new[] { 1, 2, 3, 4, 5, 1, 2, 3, 4, 5 };
        var payload = new
        {
            answers = questionIds.Select((id, i) => new { questionId = id, value = varied[i % varied.Length], durationMs = 3000 }).ToList(),
        };
        await client.PostAsJsonAsync($"/api/public/sessions/tests/{testCode}/answers", payload, TestJson.Options);
        await client.PostAsync(new Uri($"/api/public/sessions/tests/{testCode}/complete", UriKind.Relative), content: null);

        var completeSessionResponse = await client.PostAsync(new Uri("/api/public/sessions/complete", UriKind.Relative), content: null);
        completeSessionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await verifyDb.Assessments.AsNoTracking().SingleAsync(a => a.SessionToken == sessionToken);
        return assessment.ReliabilityScore!.Value;
    }

    private async Task<double> CompleteScoredPlusSuspiciousSurveySessionAsync(
        AppDbContext db, School school, string accessToken, Guid scoredTestId, Guid surveyTestId, string fullName, string programCode)
    {
        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, fullName, new DateOnly(2010, 5, 5), programCode);
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        var scoredCode = await db.TestDefinitions.AsNoTracking().Where(t => t.Id == scoredTestId).Select(t => t.Code).SingleAsync();
        await client.PostAsync(new Uri($"/api/public/sessions/tests/{scoredCode}/start", UriKind.Relative), content: null);

        var scoredQuestionIds = await db.Questions.AsNoTracking()
            .Where(q => q.TestDefinitionId == scoredTestId)
            .OrderBy(q => q.DisplayOrder)
            .Select(q => q.Id)
            .ToListAsync();

        var varied = new[] { 1, 2, 3, 4, 5, 1, 2, 3, 4, 5 };
        var scoredPayload = new
        {
            answers = scoredQuestionIds.Select((id, i) => new { questionId = id, value = varied[i % varied.Length], durationMs = 3000 }).ToList(),
        };
        await client.PostAsJsonAsync($"/api/public/sessions/tests/{scoredCode}/answers", scoredPayload, TestJson.Options);
        await client.PostAsync(new Uri($"/api/public/sessions/tests/{scoredCode}/complete", UriKind.Relative), content: null);

        var surveyCode = await db.TestDefinitions.AsNoTracking().Where(t => t.Id == surveyTestId).Select(t => t.Code).SingleAsync();
        await client.PostAsync(new Uri($"/api/public/sessions/tests/{surveyCode}/start", UriKind.Relative), content: null);

        var surveyQuestionIds = await db.Questions.AsNoTracking()
            .Where(q => q.TestDefinitionId == surveyTestId)
            .OrderBy(q => q.DisplayOrder)
            .Select(q => q.Id)
            .ToListAsync();

        // Shubhali: barchasi BIR XIL qiymat, o'ta tez (`ReliabilityCalculator` bunga kirsa
        // `AllSameAnswer`/`FastAnswers` jarimasini beradi) — bu SURVEY test, kirishga kirmasligi shart.
        var suspiciousPayload = new
        {
            answers = surveyQuestionIds.Select(id => new { questionId = id, value = 1, durationMs = 50 }).ToList(),
        };
        await client.PostAsJsonAsync($"/api/public/sessions/tests/{surveyCode}/answers", suspiciousPayload, TestJson.Options);
        await client.PostAsync(new Uri($"/api/public/sessions/tests/{surveyCode}/complete", UriKind.Relative), content: null);

        var completeSessionResponse = await client.PostAsync(new Uri("/api/public/sessions/complete", UriKind.Relative), content: null);
        completeSessionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await verifyDb.Assessments.AsNoTracking().SingleAsync(a => a.SessionToken == sessionToken);
        return assessment.ReliabilityScore!.Value;
    }

    private static async Task<string> StartSessionAsync(HttpClient client, School school, string accessToken, string fullName, DateOnly birthDate, string? programCode = null)
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
