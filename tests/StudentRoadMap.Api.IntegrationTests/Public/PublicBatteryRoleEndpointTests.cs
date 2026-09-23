using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// ⚠️ **Batareya ROLI bo'yicha tanlash** (`Domain.Catalog.PersonalityBattery.RoleOf`) — 2026-09-03
/// da tuzatilgan LATENT xato: kod "bu sessiyada shaxsiyat natijasi qaysi?" degan savolga metodika
/// KODINI satr bo'yicha solishtirib javob berardi (`TestCode == "MBTI16"` / `"RIASEC"` / `"BIG5"` /
/// `"ACTIVITY"`). Dastur (`AssessmentProgram`) tushunchasidan keyin bu JIMGINA buziladi va hech
/// qanday xato ko'rinmaydi.
///
/// <para>
/// Har ssenariyda kodlar ATAYLAB zid: haqiqiy batareya `PERS-BAT-*` kodi bilan, `MBTI16`/`RIASEC`/
/// `BIG5`/`ACTIVITY` kodlari esa `Custom` anketalarda. Satr solishtiruvchi mantiq bu holatlarda
/// albatta yiqiladi (har bir `[Fact]` izohida "eski kod nima qilardi" ko'rsatilgan).
/// Har ssenariy ALOHIDA fixture (`PublicPersonalityBatteryFlagEndpointTests` bilan bir xil sabab:
/// `Public` dastur barcha maktabda ko'rinadi).
/// </para>
/// </summary>
public sealed class PublicStudentResultBatteryRoleEndpointTests : IClassFixture<ShowResultApiTestFactory>
{
    private readonly ShowResultApiTestFactory _factory;

    public PublicStudentResultBatteryRoleEndpointTests(ShowResultApiTestFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// `GET /api/public/sessions/result` — shaxsiyat tipi va kasb yo'nalishlari BATAREYA ROLI
    /// bo'yicha tanlanadi.
    ///
    /// <para>
    /// Sessiyada 4 ta natija bor:
    /// `PERS-BAT-1` (`Standard`+`Scored`, `MBTI16` strategiyasi) → `INTJ` — HAQIQIY tip;
    /// `PERS-BAT-2` (`Standard`+`Scored`, `RIASEC` strategiyasi) → `IRA` — HAQIQIY Holland kodi;
    /// `MBTI16` KODLI `Custom` anketa → `ESFP` (chalg'ituvchi tip);
    /// `RIASEC` KODLI `Custom` anketa → `ZZZ` (Holland harflari yo'q — kasb yo'nalishi chiqmaydi).
    /// </para>
    ///
    /// <para>
    /// <b>Eski kodda:</b> `personalityType` = `ESFP` (superadmin anketasining natijasi!) va
    /// `careerFields` bo'sh — ya'ni o'quvchi butunlay boshqa odamning tipini ko'rardi.
    /// </para>
    /// </summary>
    [Fact]
    public async Task GetStudentResult_MBTI16KodliCustomAnketaBilanZid_NatijaBatareyaRolidanOlinadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        // ⚠️ Haqiqiy `DbSeeder` ATAYLAB chaqirilmaydi: u `MBTI16`/`RIASEC` KODLI TIZIM
        // metodikalarini yaratadi va shu kodlar band bo'lib qolardi — bu testning butun ma'nosi
        // esa aynan o'sha kodlarni `Custom` anketaga berishda. Shu sabab natijani ko'rsatish
        // uchun kerak bo'lgan minimal katalog (`TypeCatalog`/`CareerMap`) qo'lda kiritiladi.
        db.TypeCatalog.AddRange(
            TypeCatalogEntry.Create("INTJ", "Loyihachi", "Uzoqni ko'zlab reja tuzadi.", "Batafsil tavsif.", ["Tahlil", "Rejalashtirish"]),
            TypeCatalogEntry.Create("ESFP", "Quvnoq", "Hozirgi damda yashaydi.", "Batafsil tavsif.", ["Muloqot"]));
        db.CareerMap.Add(CareerMapEntry.Create(Guid.NewGuid(), "IR", "Muhandislik va texnika"));
        await db.SaveChangesAsync();
        var accessToken = TestDataFactory.NewAccessToken("battery-role-result1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-battery-role-result1", accessToken, showResultToStudent: true);

        // Kod ≠ strategiya: haqiqiy batareya `PERS-BAT-*` kodi bilan.
        var personalityTest = await TestDataFactory.CreateStandaloneSystemTestAsync(
            db, now, "PERS-BAT-1", 3, questionCount: 1, scoringStrategyCode: "MBTI16");
        var careerTest = await TestDataFactory.CreateStandaloneSystemTestAsync(
            db, now, "PERS-BAT-2", 4, questionCount: 1, scoringStrategyCode: "RIASEC");

        // Chalg'ituvchilar: `MBTI16`/`RIASEC` KODLI, lekin `Custom` + `SUM` anketalar.
        var decoyPersonality = await TestDataFactory.CreateStandaloneTestAsync(db, now, "MBTI16", 1, questionCount: 1);
        var decoyCareer = await TestDataFactory.CreateStandaloneTestAsync(db, now, "RIASEC", 2, questionCount: 1);

        var phone = PhoneNumber.Create("+998901234567").Value;
        var student = Student.Create(
            Guid.NewGuid(), school.Id, "Roliyev Botir Akmalovich", new DateOnly(2010, 4, 17), Gender.Male, 9, phone, now, now);
        db.Students.Add(student);

        const string sessionToken = "battery-role-result-session-token-0123456789";
        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var assessment = Assessment.Create(
            Guid.NewGuid(), student.Id, school.Id, sessionToken, "uz", programId,
            startedAt: now.AddMinutes(-20), expiresAt: now.AddDays(7), now: now);

        // ⚠️ Chalg'ituvchilar ataylab OLDINDA (`DisplayOrder` 1–2): eski `FirstOrDefault(TestCode == ...)`
        // aynan ularni topardi. Scoring ENGINE bu testda chaqirilmaydi — `TestResult`lar qo'lda
        // yoziladi, ya'ni faqat "qaysi natija tanlanadi" mantig'i izolyatsiyada sinaladi
        // (`PublicGetStudentResultEndpointTests` dagi naqsh).
        var blocks = new List<(Guid DefinitionId, AssessmentTest Test, string ResultCode)>();
        var order = 1;
        foreach (var (definition, resultCode) in new[]
                 {
                     (decoyPersonality, "ESFP"),
                     (decoyCareer, "ZZZ"),
                     (personalityTest, "INTJ"),
                     (careerTest, "IRA"),
                 })
        {
            var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, definition.Id, order++, totalCount: 1);
            assessment.AddTest(assessmentTest);
            blocks.Add((definition.Id, assessmentTest, resultCode));
        }

        var testResults = new List<TestResult>();
        foreach (var (definitionId, assessmentTest, resultCode) in blocks)
        {
            var questionId = (await db.Questions.AsNoTracking().FirstAsync(q => q.TestDefinitionId == definitionId)).Id;
            assessment.StartTest(definitionId, now);
            assessmentTest.UpsertAnswer(Guid.NewGuid(), questionId, 4, null, 2000, now);
            assessment.CompleteTest(definitionId, [questionId], now);

            var definitionCode = (await db.TestDefinitions.AsNoTracking().SingleAsync(t => t.Id == definitionId)).Code;
            testResults.Add(TestResult.Create(
                Guid.NewGuid(), assessmentTest.Id, assessment.Id, definitionCode, "{}", "{}",
                scoringVersion: 1, testVersion: 1, computedAt: now, resultCode: resultCode));
        }

        assessment.Complete(now);
        assessment.SetReliability(90.0, ReliabilityFlag.Reliable, now);
        assessment.MarkAnalyzing(now);
        assessment.MarkAnalyzed(now);

        db.Assessments.Add(assessment);
        db.TestResults.AddRange(testResults);
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        var response = await client.GetAsync(new Uri("/api/public/sessions/result", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        json.GetProperty("personalityType").GetString().Should().Be(
            "INTJ", "tip `MBTI16` strategiyali BATAREYA anketasidan olinadi — `MBTI16` KODLI `Custom` anketadan emas");
        json.GetProperty("typeName").GetString().Should().Be("Loyihachi");
        json.GetProperty("careerFields").EnumerateArray().Should().NotBeEmpty(
            "Holland kodi `RIASEC` strategiyali batareya anketasidan (`IRA`) olinadi — `RIASEC` KODLI `Custom` anketadagi `ZZZ` dan emas");
    }
}

/// <summary>
/// `MaturityIndex` (`CompositeScorer`) kirishlari ham ROL bo'yicha tanlanadi. Bu ssenariy eski
/// kodda shunchaki noto'g'ri natija emas, **ochiq xato** berardi: `BIG5`/`ACTIVITY` KODLI, lekin
/// boshqa shkalali `Custom` anketalar `CompositeScorer`ga BIG5/ACTIVITY sifatida uzatilardi va
/// `SCORING_COMPOSITE_MISSING_BIG5` (`DomainException`) bilan sessiyani yakunlash YIQILARDI.
/// </summary>
public sealed class PublicMaturityIndexBatteryRoleEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicMaturityIndexBatteryRoleEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CompleteSession_BIG5VaACTIVITYKodliCustomAnketalar_MaturityIndexHisoblanmaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("battery-role-maturity1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-battery-role-maturity1", accessToken);

        // `BIG5`/`ACTIVITY` KODLI, lekin `Custom` (`RIASEC` shaklidagi) anketalar — batareya EMAS.
        var decoyBigFive = await TestDataFactory.CreateStandaloneRiasecShapedTestAsync(db, now, "BIG5", 1);
        var decoyActivity = await TestDataFactory.CreateStandaloneRiasecShapedTestAsync(db, now, "ACTIVITY", 2);
        await TestDataFactory.CreateProgramAsync(db, now, "BATTERY-ROLE-MATURITY-PROG1", [(decoyBigFive.Id, 1), (decoyActivity.Id, 2)]);

        using var client = _factory.CreateClient();
        var sessionToken = await BatteryRoleFlow.StartSessionAsync(
            client, school, accessToken, "Yetuklik Talabasi", "BATTERY-ROLE-MATURITY-PROG1");
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        await BatteryRoleFlow.CompleteTestAsync(client, db, "BIG5", decoyBigFive.Id);
        await BatteryRoleFlow.CompleteTestAsync(client, db, "ACTIVITY", decoyActivity.Id);

        var completeSessionResponse = await client.PostAsync(new Uri("/api/public/sessions/complete", UriKind.Relative), content: null);

        completeSessionResponse.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "`BIG5`/`ACTIVITY` KODLI `Custom` anketalar `CompositeScorer`ga umuman uzatilmasligi kerak — eski kod ularni kod bo'yicha topib, `SCORING_COMPOSITE_MISSING_BIG5` bilan yiqilardi");

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await verifyDb.Assessments.AsNoTracking().SingleAsync(a => a.SessionToken == sessionToken);
        var bigFiveCodedResult = await verifyDb.TestResults.AsNoTracking()
            .SingleAsync(r => r.AssessmentId == assessment.Id && r.TestCode == "BIG5");

        bigFiveCodedResult.CompositeIndex.Should().BeNull("`Custom` anketaga `MaturityIndex` yozilmaydi");

        var student = await verifyDb.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);
        student.LastMaturityIndex.Should().BeNull();
        student.LastActivityIndex.Should().BeNull("`ACTIVITY` KODLI `Custom` anketa aktivlik indeksi manbai emas");
        student.LastActivityLevel.Should().BeNull();
    }
}

/// <summary>
/// `StudentSnapshot` (`Student.UpdateSnapshot`) maydonlari ham ROL bo'yicha to'ldiriladi:
/// batareya boshqa KOD bilan kelsa ham tip/Holland kodi yoziladi, `MBTI16` KODLI `Custom` anketa
/// esa tipni EGALLAB OLMAYDI.
/// </summary>
public sealed class PublicStudentSnapshotBatteryRoleEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicStudentSnapshotBatteryRoleEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Dasturda: `MBTI16` KODLI `Custom` anketa (birinchi — eski `FirstOrDefault` aynan shuni
    /// topardi), `PERS-BAT-1` kodli haqiqiy `MBTI16` batareyasi va `PERS-BAT-2` kodli haqiqiy
    /// `RIASEC` batareyasi.
    ///
    /// <para>
    /// <b>Eski kodda:</b> `LastPersonalityType` ga `Custom` anketaning Holland kodi (3 harfli!)
    /// yozilardi, `LastHollandCode` esa `null` qolardi (`TestCode == "RIASEC"` topilmagani uchun).
    /// </para>
    /// </summary>
    [Fact]
    public async Task CompleteSession_BatareyaBoshqaKodBilan_SnapshotRolBoyichaYangilanadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("battery-role-snapshot1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-battery-role-snapshot1", accessToken);

        var decoy = await TestDataFactory.CreateStandaloneRiasecShapedTestAsync(db, now, "MBTI16", 1);
        var personalityTest = await TestDataFactory.CreateStandaloneSystemMbtiShapedTestAsync(db, now, "PERS-BAT-1", 2);
        var careerTest = await TestDataFactory.CreateStandaloneSystemRiasecShapedTestAsync(db, now, "PERS-BAT-2", 3);

        await TestDataFactory.CreateProgramAsync(
            db, now, "BATTERY-ROLE-SNAPSHOT-PROG1", [(decoy.Id, 1), (personalityTest.Id, 2), (careerTest.Id, 3)]);

        using var client = _factory.CreateClient();
        var sessionToken = await BatteryRoleFlow.StartSessionAsync(
            client, school, accessToken, "Snapshot Rol Talabasi", "BATTERY-ROLE-SNAPSHOT-PROG1");
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        await BatteryRoleFlow.CompleteTestAsync(client, db, "MBTI16", decoy.Id);
        await BatteryRoleFlow.CompleteTestAsync(client, db, "PERS-BAT-1", personalityTest.Id);
        await BatteryRoleFlow.CompleteTestAsync(client, db, "PERS-BAT-2", careerTest.Id);

        var completeSessionResponse = await client.PostAsync(new Uri("/api/public/sessions/complete", UriKind.Relative), content: null);
        completeSessionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await verifyDb.Assessments.AsNoTracking().SingleAsync(a => a.SessionToken == sessionToken);
        var results = await verifyDb.TestResults.AsNoTracking().Where(r => r.AssessmentId == assessment.Id).ToListAsync();

        // Kutilgan qiymatlar HAQIQIY natijalardan olinadi (formulaga qotirib bog'lanmaslik uchun).
        var expectedPersonalityType = results.Single(r => r.TestCode == "PERS-BAT-1").ResultCode;
        var expectedHollandCode = results.Single(r => r.TestCode == "PERS-BAT-2").ResultCode;
        var decoyResultCode = results.Single(r => r.TestCode == "MBTI16").ResultCode;

        expectedPersonalityType.Should().HaveLength(4, "`MBTI16` strategiyasi 4 harfli tip beradi (`docs/03` §2.3)");
        expectedPersonalityType.Should().NotBe(decoyResultCode, "ssenariyning ma'nosi: ikki qiymat ATAYLAB har xil");

        var student = await verifyDb.Students.AsNoTracking().SingleAsync(s => s.Id == assessment.StudentId);

        student.LastPersonalityType.Should().Be(
            expectedPersonalityType, "tip `MBTI16` STRATEGIYASI bilan ballangan batareyadan keladi, `MBTI16` KODLI anketadan emas");
        student.LastHollandCode.Should().Be(
            expectedHollandCode, "Holland kodi `RIASEC` strategiyali batareyadan keladi, kodi `RIASEC` bo'lmasa ham");
    }
}

/// <summary>Batareya roli testlaridagi takrorlanadigan HTTP oqimi (sessiya boshlash va bitta test blokini to'liq yechish).</summary>
internal static class BatteryRoleFlow
{
    public static async Task<string> StartSessionAsync(
        HttpClient client, School school, string accessToken, string fullName, string programCode)
    {
        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, fullName, new DateOnly(2010, 5, 5), Gender.Male, 9, "A",
            "+998901234567", "+998909998877", null, true, "uz", programCode);

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);
        return body!.SessionToken;
    }

    /// <summary>Bitta test blokini boshlash → barcha savolga javob → yakunlash (javoblar `SaveAnswersCommandValidator.MaxAnswersPerRequest` bo'yicha bo'laklanadi).</summary>
    public static async Task CompleteTestAsync(HttpClient client, AppDbContext db, string testCode, Guid testDefinitionId)
    {
        var startResponse = await client.PostAsync(new Uri($"/api/public/sessions/tests/{testCode}/start", UriKind.Relative), content: null);
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var questionIds = await db.Questions.AsNoTracking()
            .Where(q => q.TestDefinitionId == testDefinitionId)
            .OrderBy(q => q.DisplayOrder)
            .Select(q => q.Id)
            .ToListAsync();

        foreach (var chunk in questionIds.Chunk(40))
        {
            var payload = new { answers = chunk.Select(id => new { questionId = id, value = 3, durationMs = 3000 }).ToList() };
            var saveResponse = await client.PostAsJsonAsync($"/api/public/sessions/tests/{testCode}/answers", payload, TestJson.Options);
            saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var completeResponse = await client.PostAsync(new Uri($"/api/public/sessions/tests/{testCode}/complete", UriKind.Relative), content: null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
