using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// ⚠️ `GET /api/admin/students/{id}` — `latestAssessment.results` bloklari batareya ROLI
/// (`PersonalityBattery.RoleOf`, ya'ni `ScoringStrategyCode`) bo'yicha to'ldiriladi, metodika
/// KODI bo'yicha EMAS (2026-09-03 da tuzatilgan oxirgi qoldiq —
/// `StudentProfileMapping.BuildTestResultsAsync` da `TestCode == "MBTI16"` satr solishtiruvi).
///
/// <para>
/// Kodlar ATAYLAB zid: haqiqiy batareya `PERS-BAT-1`/`PERS-BAT-3` kodli (`Standard` + `Scored` +
/// `MBTI16`/`RIASEC` strategiyalari), chalg'ituvchi esa `MBTI16`/`RIASEC` KODLI `Custom` (`SUM`)
/// superadmin anketasi. <b>Eski kodda</b> `results.MBTI16.resultCode` = `ESFP` (typeName
/// "Chalg'ituvchi tip") va `results.RIASEC.resultCode` = `ZZZ` (kasb yo'nalishlari bo'sh)
/// chiqardi — hech qanday xatosiz.
/// </para>
///
/// <para>
/// Javob XOM JSON matni ustidan tekshiriladi: `ReadFromJsonAsync&lt;Dto&gt;` kalit farqini
/// KO'RMAYDI (2026-09-03 qarori), kalitlar esa shartnoma —
/// `MBTI16`/`BIG5`/`RIASEC`/`ACTIVITY` (anketa kodlari boshqacha bo'lsa ham).
/// </para>
///
/// <para>
/// Alohida `IClassFixture` — bu test `MBTI16`/`RIASEC` KODLARINI `Custom` anketalarga beradi,
/// haqiqiy `DbSeeder` esa aynan shu kodli TIZIM metodikalarini yaratadi
/// (`ux_test_definitions_code` to'qnashuvi). Shu sabab seeder chaqirilmaydi, natijani ko'rsatish
/// uchun kerak bo'lgan minimal katalog qo'lda kiritiladi (`PublicBatteryRoleEndpointTests` naqshi).
/// </para>
/// </summary>
public sealed class AdminStudentProfileBatteryRoleEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminStudentProfileBatteryRoleEndpointTests(PublicApiTestFactory factory)
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
    public async Task GetById_AnketaKodiZid_ResultsBloklariBatareyaRolidanToLdiriladi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        db.TypeCatalog.AddRange(
            TypeCatalogEntry.Create("INTJ", "Loyihachi", "Qisqa tavsif", "Uzun tavsif"),
            TypeCatalogEntry.Create("ESFP", "Chalg'ituvchi tip", "Qisqa tavsif", "Uzun tavsif"));
        db.CareerMap.Add(CareerMapEntry.Create(Guid.NewGuid(), "IR", "Muhandislik va texnika", 1, null, ["Muhandis"]));
        await db.SaveChangesAsync();

        var school = await TestDataFactory.CreateSchoolAsync(
            db, now, "admin-battery-role", TestDataFactory.NewAccessToken("admin-battery-role"));

        // Haqiqiy batareya — kodi `PERS-BAT-*`.
        var batteryPersonality = await TestDataFactory.CreateStandaloneSystemTestAsync(
            db, now, "PERS-BAT-1", 3, questionCount: 1, scoringStrategyCode: "MBTI16");
        var batteryCareer = await TestDataFactory.CreateStandaloneSystemTestAsync(
            db, now, "PERS-BAT-3", 4, questionCount: 1, scoringStrategyCode: "RIASEC");

        // Chalg'ituvchilar — batareya KODLARI, lekin `Custom` + `SUM`.
        var decoyPersonality = await TestDataFactory.CreateStandaloneTestAsync(db, now, "MBTI16", 1, questionCount: 1);
        var decoyCareer = await TestDataFactory.CreateStandaloneTestAsync(db, now, "RIASEC", 2, questionCount: 1);

        var phone = PhoneNumber.Create("+998901234599").Value;
        var student = Student.Create(
            Guid.NewGuid(), school.Id, "Roliyeva Nigora Akmalovna", new DateOnly(2010, 4, 17), Gender.Female, 9, phone, now, now);
        db.Students.Add(student);

        const string sessionToken = "admin-battery-role-session-token-0123456789";
        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var assessment = Assessment.Create(
            Guid.NewGuid(), student.Id, school.Id, sessionToken, "uz", programId,
            startedAt: now.AddMinutes(-25), expiresAt: now.AddDays(7), now: now);

        // ⚠️ Chalg'ituvchilar ataylab OLDINDA — eski `FirstOrDefault(r => r.TestCode == ...)`
        // aynan ularni birinchi topardi.
        var blocks = new List<(TestDefinition Definition, string ResultCode, string NormalizedScores, string Levels)>
        {
            (decoyPersonality, "ESFP", """{"EI":90,"SN":90,"TF":90,"JP":90}""", """{"EI":"E","SN":"S","TF":"F","JP":"P"}"""),
            (decoyCareer, "ZZZ", """{"R":10,"I":10,"ART":10,"SOC":10,"ENT":10,"CONV":10,"DIFFERENTIATION":0}""", """{"CONSISTENCY":"Past"}"""),
            (batteryPersonality, "INTJ", """{"EI":28.3,"SN":71.6,"TF":33.3,"JP":64.1}""", """{"EI":"I","SN":"N","TF":"T","JP":"J"}"""),
            (batteryCareer, "IRA", """{"R":80,"I":80,"ART":80,"SOC":10,"ENT":10,"CONV":10,"DIFFERENTIATION":70}""", """{"CONSISTENCY":"Yuqori"}"""),
        };

        // Bloklar AVVAL to'liq biriktiriladi (`Draft` holatida), keyin yechiladi.
        var order = 1;
        var assessmentTests = new List<AssessmentTest>();
        foreach (var (definition, _, _, _) in blocks)
        {
            var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, definition.Id, order++, totalCount: 1);
            assessment.AddTest(assessmentTest);
            assessmentTests.Add(assessmentTest);
        }

        var testResults = new List<TestResult>();
        for (var i = 0; i < blocks.Count; i++)
        {
            var (definition, resultCode, normalizedScores, levels) = blocks[i];
            var assessmentTest = assessmentTests[i];

            var questionId = (await db.Questions.AsNoTracking().FirstAsync(q => q.TestDefinitionId == definition.Id)).Id;
            assessment.StartTest(definition.Id, now);
            assessmentTest.UpsertAnswer(Guid.NewGuid(), questionId, 4, null, 2000, now);
            assessment.CompleteTest(definition.Id, [questionId], now);

            testResults.Add(TestResult.Create(
                Guid.NewGuid(), assessmentTest.Id, assessment.Id, definition.Code, "{}", normalizedScores,
                scoringVersion: 1, testVersion: 1, computedAt: now, resultCode: resultCode, levelsJson: levels));
        }

        assessment.Complete(now);
        assessment.SetReliability(88.0, ReliabilityFlag.Reliable, now);

        db.Assessments.Add(assessment);
        db.TestResults.AddRange(testResults);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("admin-battery-role-admin");

        var response = await client.GetAsync(new Uri($"/api/admin/students/{student.Id}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // XOM JSON: kalitlar shartnoma, `ReadFromJsonAsync<Dto>` bu farqni ko'rmaydi.
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var results = document.RootElement.GetProperty("latestAssessment").GetProperty("results");

        var mbti = results.GetProperty("MBTI16");
        mbti.GetProperty("resultCode").GetString().Should().Be(
            "INTJ", "tip `MBTI16` STRATEGIYALI batareyadan olinadi — `MBTI16` KODLI `Custom` anketadan emas (eski kodda `ESFP`)");
        mbti.GetProperty("typeName").GetString().Should().Be("Loyihachi");
        mbti.GetProperty("axes").GetProperty("EI").GetProperty("letter").GetString().Should().Be("I");

        var riasec = results.GetProperty("RIASEC");
        riasec.GetProperty("resultCode").GetString().Should().Be("IRA", "eski kodda `ZZZ` chiqardi");
        riasec.GetProperty("careerFields").EnumerateArray().Should().NotBeEmpty();

        // Sessiyada `BIG5`/`ACTIVITY` ROLIDAGI anketa YO'Q — bloklar `null` (bo'sh yoki nol
        // qiymatli obyekt EMAS, `docs/06` qarorlar jurnali 2026-09-02).
        results.GetProperty("BIG5").ValueKind.Should().Be(JsonValueKind.Null);
        results.GetProperty("ACTIVITY").ValueKind.Should().Be(JsonValueKind.Null);
    }
}
