using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Ai;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
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
/// `Ai:AutoAnalyzeOnCompletion` — AI tahlilini sessiya yakunlangach AVTOMATIK navbatga qo'yish
/// bayrog'i (`docs/06` 8-bo'lim, 2026-09-03 loyiha EGASI qarori: har tahlil AI xarajati, shu
/// sabab tahlilni egasi admin panelidagi tugma orqali O'ZI ishga tushiradi).
/// Standart qiymat — **`false`** (`PublicApiTestFactory`da hech narsa berilmaydi).
/// </summary>
public sealed class PublicAutoAnalyzeDisabledEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicAutoAnalyzeDisabledEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CompleteSession_BayroqStandartOchiq_SessiyaCompletedQoladiVaNavbatgaHechNarsaQoyilmaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sessionToken = await AutoAnalyzeFlagFlow.RunSessionAsync(_factory, db, "auto-off1", "AUTOOFF1", "AUTO-OFF-PROG1");

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await verifyDb.Assessments.AsNoTracking().SingleAsync(a => a.SessionToken == sessionToken);

        assessment.Status.Should().Be(AssessmentStatus.Completed, "bayroq o'chiq — sessiya `Analyzing`ga o'tmaydi");

        var jobs = await verifyDb.AnalysisJobs.AsNoTracking().Where(j => j.AssessmentId == assessment.Id).ToListAsync();
        jobs.Should().BeEmpty("avtomatik oqim o'chirilgan — AI vazifasi navbatga QO'YILMAYDI");

        // Yakunlash mantiqi (ballar/ishonchlilik) bayroqdan mustaqil — o'zgarishsiz ishlaydi.
        assessment.CompletedAt.Should().NotBeNull();
        assessment.ReliabilityScore.Should().NotBeNull();
    }
}

/// <summary>
/// Bayroq YOQILGAN (`Ai:AutoAnalyzeOnCompletion=true`, `AutoAnalyzeApiTestFactory`) — 2026-09-03
/// gacha bo'lgan xatti-harakat: sessiya `Analyzing`ga o'tadi va AI vazifasi navbatga tushadi.
/// ⚠️ P18-R1: vazifa yozuvi javob qaytishidan oldin, ya'ni tranzaksiya COMMIT bo'lgandan keyin
/// (`IPostCommitActions`) yaratiladi — shu sabab bu yerda kutish/polling KERAK EMAS.
/// </summary>
public sealed class PublicAutoAnalyzeEnabledEndpointTests : IClassFixture<AutoAnalyzeApiTestFactory>
{
    private readonly AutoAnalyzeApiTestFactory _factory;

    public PublicAutoAnalyzeEnabledEndpointTests(AutoAnalyzeApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CompleteSession_BayroqYoqilgan_AnalyzingHolatiVaNavbatdagiVazifa()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sessionToken = await AutoAnalyzeFlagFlow.RunSessionAsync(
            _factory, db, "auto-on1", "AUTOON1", "AUTO-ON-PROG1", expectedStatus: nameof(AssessmentStatus.Analyzing));

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await verifyDb.Assessments.AsNoTracking().SingleAsync(a => a.SessionToken == sessionToken);

        // Holatning O'ZI bu yerda tekshirilmaydi: xostda haqiqiy `AnalysisWorkerBackgroundService`
        // ishlaydi va vazifani darhol olib, sessiyani `Analyzed`/`AnalysisFailed`ga o'tkazishi
        // mumkin. Determinlashgan dalil — javob tanasi (yuqorida) va NAVBATDAGI YOZUV (pastda).
        var jobs = await verifyDb.AnalysisJobs.AsNoTracking().Where(j => j.AssessmentId == assessment.Id).ToListAsync();
        jobs.Should().ContainSingle("bayroq yoqilganda AI vazifasi commit'dan keyin navbatga qo'yiladi (P18-R1)");
    }
}

/// <summary>
/// ⚠️ Qo'lda ishga tushirish (`POST /api/admin/assessments/{id}/rerun-analysis`) bayroqdan
/// MUSTAQIL: bayroq STANDART (`false`) muhitda ham tugma har doim navbatga qo'yadi. Bayroq
/// faqat AVTOMATIK oqimga (`CompleteSessionCommandHandler`) tegishli.
/// </summary>
public sealed class AdminRerunAnalysisIgnoresAutoAnalyzeFlagEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminRerunAnalysisIgnoresAutoAnalyzeFlagEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RerunAnalysis_BayroqOchiqBolsaHam_NavbatgaQoyiladi()
    {
        Guid assessmentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sessionToken = await AutoAnalyzeFlagFlow.RunSessionAsync(
                _factory, db, "rerun-off1", "RERUNOFF1", "RERUN-OFF-PROG1", usePersonalityBatteryTest: true);

            var assessment = await db.Assessments.AsNoTracking().SingleAsync(a => a.SessionToken == sessionToken);
            assessmentId = assessment.Id;

            (await db.AnalysisJobs.AsNoTracking().AnyAsync(j => j.AssessmentId == assessmentId))
                .Should().BeFalse("avtomatik oqim o'chiq — tugma bosilgunga qadar navbatda hech narsa yo'q");
        }

        using var adminClient = await AuthenticatedAdminClientAsync("auto-analyze-rerun-admin");

        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/assessments/{assessmentId}/rerun-analysis",
            new { provider = (string?)null, promptVersion = (string?)null },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var body = await response.Content.ReadFromJsonAsync<RerunAnalysisResultDto>(TestJson.Options);
        body!.Status.Should().Be(nameof(AssessmentStatus.Analyzing), "qo'lda ishga tushirish bayroqdan mustaqil");

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await verifyDb.AnalysisJobs.AsNoTracking().CountAsync(j => j.AssessmentId == assessmentId))
            .Should().Be(1, "tugma AI vazifasini navbatga qo'yadi — bayroq qiymatidan qat'i nazar");
    }

    private async Task<HttpClient> AuthenticatedAdminClientAsync(string username)
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
}

/// <summary>
/// Uch ssenariy uchun umumiy, ENG YENGIL sessiya oqimi: bitta `Survey` anketasidan iborat
/// dastur (`PublicSurveyOnlySessionEndpointTests` naqshi) — bayroq mantiqi test bankiga
/// bog'liq emas, shu sabab 190 savolli to'liq oqim bu yerda kerak emas.
/// </summary>
internal static class AutoAnalyzeFlagFlow
{
    public static async Task<string> RunSessionAsync(
        PublicApiTestFactory factory,
        AppDbContext db,
        string seed,
        string testCode,
        string programCode,
        string expectedStatus = nameof(AssessmentStatus.Completed),
        bool usePersonalityBatteryTest = false)
    {
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken(seed);
        var school = await TestDataFactory.CreateSchoolAsync(db, now, $"maktab-{seed}", accessToken);

        // `usePersonalityBatteryTest` (code-review, 2026-09-14): `RerunAnalysisCommandHandler`
        // endi server-tomonida `PersonalityBattery.Includes`ni tekshiradi — `Survey` testli
        // sessiyada qo'lda qayta tahlil `409 ASSESSMENT_NO_PERSONALITY_BATTERY` bilan to'xtaydi.
        // `AdminRerunAnalysisIgnoresAutoAnalyzeFlagEndpointTests` bayroq mustaqilligini
        // tekshiradi, batareya mavjudligini emas — shu sabab u `Standard`+`Scored` (`Kind`)
        // test so'raydi.
        // `CreateStandaloneSystemTestAsync` (default `SUM` strategiyasi) bu yerga mos EMAS: `SUM`
        // har shkalada kamida 4 savol va `InterpretationBands` talab qiladi (`docs/03` §6.3) — bu
        // fixture faqat "battery bormi" bayrog'i uchun, shu sabab tayyor `MBTI16` shakli
        // (`EI`/`SN`/`TF`/`JP` x2, band talab qilmaydi) ishlatiladi.
        var test = usePersonalityBatteryTest
            ? await TestDataFactory.CreateStandaloneSystemMbtiShapedTestAsync(db, now, testCode, displayOrder: 1)
            : await TestDataFactory.CreateStandaloneTestAsync(
                db, now, testCode, 1, questionCount: 3, scoringMode: TestScoringMode.Survey, scoringStrategyCode: null);
        await TestDataFactory.CreateProgramAsync(db, now, programCode, [(test.Id, 1)]);

        using var client = factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, $"Bayroq Talabasi {seed}", programCode);
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        (await client.PostAsync(new Uri($"/api/public/sessions/tests/{testCode}/start", UriKind.Relative), content: null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var questionIds = await db.Questions.AsNoTracking()
            .Where(q => q.TestDefinitionId == test.Id)
            .OrderBy(q => q.DisplayOrder)
            .Select(q => q.Id)
            .ToListAsync();

        var payload = new { answers = questionIds.Select(id => new { questionId = id, value = 3, durationMs = 3000 }).ToList() };
        (await client.PostAsJsonAsync($"/api/public/sessions/tests/{testCode}/answers", payload, TestJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var completeTestResponse = await client.PostAsync(new Uri($"/api/public/sessions/tests/{testCode}/complete", UriKind.Relative), content: null);
        completeTestResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await completeTestResponse.Content.ReadFromJsonAsync<CompleteTestResult>(TestJson.Options))!
            .AllTestsCompleted.Should().BeTrue();

        var completeSessionResponse = await client.PostAsync(new Uri("/api/public/sessions/complete", UriKind.Relative), content: null);
        completeSessionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var completeSessionBody = await completeSessionResponse.Content.ReadFromJsonAsync<CompleteSessionResult>(TestJson.Options);
        completeSessionBody!.Status.Should().Be(expectedStatus);

        return sessionToken;
    }

    private static async Task<string> StartSessionAsync(HttpClient client, School school, string accessToken, string fullName, string programCode)
    {
        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, fullName, new DateOnly(2010, 5, 5), Gender.Male, 9, "A",
            "+998901234567", null, null, true, "uz", programCode);

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);
        return body!.SessionToken;
    }
}
