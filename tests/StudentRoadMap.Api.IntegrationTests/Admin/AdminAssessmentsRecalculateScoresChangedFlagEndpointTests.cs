using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Assessments;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `AssessmentsController.RecalculateScores` — `Changed` bayrog'i. QA topilmasi (2026-09-02):
/// avval `Changed` faqat `ResultCode`/`CompositeIndex`/ishonchlilikni solishtirar edi — xom/
/// normallashgan ballar (yoki `ScoringVersion`) o'zgarib, yuqori darajadagi natija kodi VA
/// ishonchlilik o'zgarmasa, `Changed=false` NOTO'G'RI qaytardi.
/// `RecalculateAssessmentScoresCommandHandler` endi TO'LIQ payload (xom/normallashgan ballar,
/// darajalar, bayroqlar, `ScoringVersion`) bo'yicha solishtiradi.
///
/// Bu testni ANIQ (natija kodi VA ishonchlilik o'zgarmaydigan, lekin ballar o'zgaradigan)
/// holatga moslash uchun javoblar ATAYLAB navbatlashtirilgan (2,4,2,4,...) — bir xil emas
/// (`AllSameAnswer`/straight-lining ishga tushmaydi, `docs/03` §7), shu bilan bitta javobni
/// (2→3) o'zgartirish ISHONCHLILIKKA (5 jarima signalidan birortasiga ham) TA'SIR QILMAYDI:
/// `AllSameAnswer`/`StraightLining` — allaqachon yolg'on (aralash qiymatlar) va shunday qoladi
/// (uzun ketma-ketlik hosil bo'lmaydi); `FastAnswers` — `durationMs` o'zgarmaydi;
/// `ReverseConflict` — RIASEC'da teskari yo'nalishli savol YO'Q (har doim 0); `ShortSession` —
/// sessiya davomiyligi o'zgarmaydi. Barcha 6 tip DASTLAB teng xom ball (24) bilan boshlanadi —
/// `TypeOrder` ustuvorligi bo'yicha teng holatda "R" ustuvor (top-3 ichida), kichik o'sish uni
/// FAQAT mustahkamlaydi (o'rnini o'zgartirmaydi) — natija kodi ham bir xil qoladi.
///
/// `AdminAssessmentsRecalculateScoresEndpointTests`dan ALOHIDA sinf — ikkalasi ham
/// `TestDefinition.Code = "RIASEC"` talab qiladi (`StudentProfileMapping` bu kodni qattiq
/// kodlangan holda qidiradi — `results.riasec`ni to'ldirish uchun), bitta DB'da
/// (`ux_test_definitions_code`) ikkalasi bo'lolmaydi — shu sabab alohida `IClassFixture`.
/// </summary>
public sealed class AdminAssessmentsRecalculateScoresChangedFlagEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminAssessmentsRecalculateScoresChangedFlagEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
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

    [Fact]
    public async Task RecalculateScores_NatijaKodiVaIshonchlilikOzgarmasaHamBallarOzgarsa_ChangedTrueQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("recalc-changed");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-recalc-changed", accessToken);
        var testDefinition = await TestDataFactory.CreatePublishedRiasecShapedTestWithOptionalExtrasAsync(db, now, "RIASEC", 1, extraOptionalCount: 0);

        using var publicClient = _factory.CreateClient();
        var startCommand = new StartSessionCommand(
            school.Slug.Value, accessToken, null, "Ochilova Kamila Rustamovna", new DateOnly(2010, 4, 4), Gender.Female, 9, "A",
            "+998907774003", null, null, true, "uz");
        var startResponse = await publicClient.PostAsJsonAsync("/api/public/sessions", startCommand, TestJson.Options);
        startResponse.EnsureSuccessStatusCode();
        var sessionToken = (await startResponse.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!.SessionToken;
        publicClient.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        await publicClient.PostAsync(new Uri("/api/public/sessions/tests/RIASEC/start", UriKind.Relative), content: null);

        // Har tip ichida navbatlashgan (2,4,2,4,...) javoblar — barcha 6 tip TENG xom ball
        // (24) bilan boshlanadi, `AllSameAnswer`/`StraightLining` ishga tushmaydi.
        var orderedQuestions = testDefinition.Questions.OrderBy(q => q.DisplayOrder).ToList();
        var answersToSend = orderedQuestions
            .Select((q, index) => new { questionId = q.Id, value = index % 8 % 2 == 0 ? 2 : 4, durationMs = 1000 })
            .ToList();
        (await publicClient.PostAsJsonAsync("/api/public/sessions/tests/RIASEC/answers", new { answers = answersToSend }, TestJson.Options)).EnsureSuccessStatusCode();

        (await publicClient.PostAsync(new Uri("/api/public/sessions/tests/RIASEC/complete", UriKind.Relative), content: null)).EnsureSuccessStatusCode();
        (await publicClient.PostAsync(new Uri("/api/public/sessions/complete", UriKind.Relative), content: null)).EnsureSuccessStatusCode();

        Guid assessmentId;
        double? originalReliability;
        using (var lookupScope = _factory.Services.CreateScope())
        {
            var lookupDb = lookupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var assessment = await lookupDb.Assessments.AsNoTracking().SingleAsync(a => a.SessionToken == sessionToken);
            assessmentId = assessment.Id;
            originalReliability = assessment.ReliabilityScore;
        }

        Guid firstRQuestionId;
        double originalRTypeScore;
        using (var lookupScope = _factory.Services.CreateScope())
        {
            var lookupDb = lookupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            firstRQuestionId = (await lookupDb.Questions.AsNoTracking()
                .Where(q => q.TestDefinitionId == testDefinition.Id && q.Scale == "R")
                .OrderBy(q => q.DisplayOrder)
                .FirstAsync()).Id;

            var testResult = await lookupDb.TestResults.AsNoTracking().SingleAsync(r => r.AssessmentId == assessmentId);
            originalRTypeScore = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(testResult.NormalizedScoresJson)!["R"];
        }

        // Sessiya yakunlangach — bitta "R" javobini kichik miqdorda (2→3) o'zgartiramiz
        // (to'g'ridan-to'g'ri DB orqali, admin API'da javob tahrirlash yo'q; `durationMs`
        // O'ZGARTIRILMAYDI — sinf izohidagi ishonchlilikni o'zgarmas qoldirish sharti).
        using (var mutateScope = _factory.Services.CreateScope())
        {
            var mutateDb = mutateScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var answer = await mutateDb.Answers.SingleAsync(a => a.QuestionId == firstRQuestionId);
            answer.UpdateValue(3, null, answer.DurationMs, now);
            await mutateDb.SaveChangesAsync();
        }

        using var adminClient = await AuthenticatedAdminClientAsync("assessments-recalc-changed-admin");

        var response = await adminClient.PostAsync(new Uri($"/api/admin/assessments/{assessmentId}/recalculate-scores", UriKind.Relative), content: null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = (await response.Content.ReadFromJsonAsync<AdminRecalculateScoresResultDto>(TestJson.Options))!;

        // Natija kodi va ishonchlilik BIR XIL qoladi (sinf izohidagi dizayn) — aynan shu holat
        // eski (buzuq) `Changed` hisobini "false" deb yozdirar edi.
        result.ReliabilityScore.Should().Be(originalReliability, "sinf izohidagi dizayn bo'yicha ishonchlilikka ta'sir qilmasligi kerak");
        result.Results.Riasec.Should().NotBeNull();
        result.Results.Riasec!.Types["R"].Should().BeGreaterThan(originalRTypeScore, "bitta R javobi 2 dan 3 ga oshirildi");

        // Aynan shu — natija kodi/ishonchlilik bir xil bo'lsa ham — `Changed=true` bo'lishi shart.
        result.Changed.Should().BeTrue(
            "xom/normallashgan ballar o'zgardi (R shkalasi) — natija kodi/ishonchlilik o'zgarmagan bo'lsa ham `Changed` buni aks ettirishi kerak");
    }
}
