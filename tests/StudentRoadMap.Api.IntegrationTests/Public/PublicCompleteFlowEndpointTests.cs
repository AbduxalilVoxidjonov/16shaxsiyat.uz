using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.CompleteSession;
using StudentRoadMap.Application.Public.CompleteTest;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Persistence.Seeding;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// `POST .../tests/{testCode}/complete` va `POST /sessions/complete` — `docs/07` 1.7/1.8-bo'lim,
/// `prompts/12`. To'liq oqim: sessiya → haqiqiy 190 savolli test banki (`DbSeeder`, P05–P08)
/// bo'yicha 4 testni to'ldirish → yakunlash → `TestResult`lar, `MaturityIndex`, `ReliabilityScore`.
/// Alohida `IClassFixture` — o'zining `PublicApiTestFactory` nusxasi (rate limiter boshqa
/// testlar bilan aralashmasligi uchun, `PublicForwardedHeadersTests` izohidagi naqsh).
/// </summary>
public sealed class PublicCompleteFlowEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicCompleteFlowEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ToLiqOqim_TortTestniToldirishVaYakunlash_TestResultlarMaturityIndexVaReliabilityScoreYoziladi_VaCompleteSessionIdempotent()
    {
        using (var seedScope = _factory.Services.CreateScope())
        {
            var seeder = seedScope.ServiceProvider.GetRequiredService<DbSeeder>();
            await seeder.SeedAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("flow1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-flow1", accessToken);

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Egamov Diyorbek Shuxratovich", new DateOnly(2010, 1, 1));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        var testCodesInOrder = new[] { "MBTI16", "BIG5", "RIASEC", "ACTIVITY" };
        CompleteTestResult? lastCompleteTestBody = null;
        CompleteTestResult? firstCompleteTestBody = null;

        foreach (var testCode in testCodesInOrder)
        {
            var startResponse = await client.PostAsync(new Uri($"/api/public/sessions/tests/{testCode}/start", UriKind.Relative), content: null);
            startResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"'{testCode}' boshlanishi kerak");

            var testDefinitionId = await db.TestDefinitions.AsNoTracking()
                .Where(t => t.Code == testCode)
                .Select(t => t.Id)
                .SingleAsync();

            var questionIds = await db.Questions.AsNoTracking()
                .Where(q => q.TestDefinitionId == testDefinitionId && q.IsActive)
                .OrderBy(q => q.DisplayOrder)
                .Select(q => q.Id)
                .ToListAsync();

            // 190 savolli haqiqiy bankda MBTI16=60 ta — `SaveAnswersCommandValidator.MaxAnswersPerRequest`
            // (50) dan oshadi, shu sabab 50talik paketlarga bo'linadi (barcha test uchun umumiy yo'l).
            foreach (var chunk in questionIds.Chunk(50))
            {
                var payload = new
                {
                    answers = chunk.Select(id => new { questionId = id, value = 3, durationMs = 3000 }).ToList(),
                };

                var saveResponse = await client.PostAsJsonAsync($"/api/public/sessions/tests/{testCode}/answers", payload, TestJson.Options);
                saveResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"'{testCode}' javoblari saqlanishi kerak");
            }

            var completeResponse = await client.PostAsync(new Uri($"/api/public/sessions/tests/{testCode}/complete", UriKind.Relative), content: null);
            completeResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"'{testCode}' yakunlanishi kerak");

            var body = await completeResponse.Content.ReadFromJsonAsync<CompleteTestResult>(TestJson.Options);
            firstCompleteTestBody ??= body;
            lastCompleteTestBody = body;
        }

        // `docs/07` 1.7-bo'lim: birinchi test yakunlanganda `nextTestCode` keyingisini ko'rsatadi,
        // `allTestsCompleted=false`; oxirgisida `nextTestCode=null`, `allTestsCompleted=true`.
        firstCompleteTestBody!.TestCode.Should().Be("MBTI16");
        firstCompleteTestBody.NextTestCode.Should().Be("BIG5");
        firstCompleteTestBody.AllTestsCompleted.Should().BeFalse();

        lastCompleteTestBody!.TestCode.Should().Be("ACTIVITY");
        lastCompleteTestBody.NextTestCode.Should().BeNull();
        lastCompleteTestBody.AllTestsCompleted.Should().BeTrue();

        // --- `POST /api/public/sessions/complete` ---
        var completeSessionResponse = await client.PostAsync(new Uri("/api/public/sessions/complete", UriKind.Relative), content: null);
        completeSessionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var completeSessionBody = await completeSessionResponse.Content.ReadFromJsonAsync<CompleteSessionResult>(TestJson.Options);
        completeSessionBody!.Status.Should().Be("Analyzing");
        completeSessionBody.ShowResultToStudent.Should().BeFalse("standart `App:ShowResultToStudent` `false` (`prompts/12` cheklovi 8)");

        using (var verifyScope = _factory.Services.CreateScope())
        {
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var assessment = await verifyDb.Assessments.AsNoTracking().SingleAsync(a => a.SessionToken == sessionToken);

            assessment.Status.Should().Be(AssessmentStatus.Analyzing);
            assessment.TotalDurationSeconds.Should().NotBeNull();
            // Aniq son (masalan `30.0`) bilan tekshirmaymiz — bu haqiqiy devor-soati vaqtiga
            // (`ShortSession` jarimasi 6 daqiqadan qisqa bo'lsa) bog'liq bo'lib, sekin CI'da
            // beqaror bo'lishi mumkin edi. DoD talabi — "yozilgan" (`null` emas).
            assessment.ReliabilityScore.Should().NotBeNull();
            assessment.ReliabilityFlag.Should().NotBeNull();

            var testResults = await verifyDb.TestResults.AsNoTracking().Where(r => r.AssessmentId == assessment.Id).ToListAsync();
            testResults.Should().HaveCount(4);
            testResults.Select(r => r.TestCode).Should().BeEquivalentTo(testCodesInOrder);

            // Barcha javob "3" (neytral) — `BigFiveStrategy`/`ActivityStrategy` "AllNeutral" oltin
            // testlari bilan bir xil: har shkala 50%, shuning uchun `MaturityIndex` ANIQ 50.0
            // (`docs/03` §3.3 og'irliklari yig'indisi 1.0 — 0.3*50+0.25*50+0.2*50+0.15*50+0.1*50=50).
            var bigFiveResult = testResults.Single(r => r.TestCode == "BIG5");
            bigFiveResult.CompositeIndex.Should().Be(50.0, "P12-R... emas, lekin `CompositeScorer.ApplyMaturityIndex` BIG5 natijasiga MaturityIndex'ni yozishi shart");

            var activityResult = testResults.Single(r => r.TestCode == "ACTIVITY");
            activityResult.CompositeIndex.Should().Be(50.0, "ActivityStrategy o'z CompositeIndex'ini (ActivityIndex) hisoblaydi");
        }

        // --- Idempotentlik: `CompleteSession` ikkinchi marta chaqirilsa xato bermaydi ---
        var secondCompleteSessionResponse = await client.PostAsync(new Uri("/api/public/sessions/complete", UriKind.Relative), content: null);
        secondCompleteSessionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondBody = await secondCompleteSessionResponse.Content.ReadFromJsonAsync<CompleteSessionResult>(TestJson.Options);
        secondBody!.Status.Should().Be("Analyzing");
    }

    private static async Task<string> StartSessionAsync(HttpClient client, School school, string accessToken, string fullName, DateOnly birthDate)
    {
        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, fullName, birthDate, Gender.Male, 9, "A",
            "+998901234567", null, null, true, "uz");

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);
        return body!.SessionToken;
    }
}

/// <summary>`CompleteTest` — majburiy savollar to'liq javoblanmasa `400 VALIDATION_ERROR` + `unansweredCount`. Alohida `IClassFixture`.</summary>
public sealed class PublicCompleteTestValidationEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicCompleteTestValidationEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CompleteTest_MajburiySavollarJavoblanmagan_400VaUnansweredCountQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("unans1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-unans1", accessToken);
        var testDefinition = await TestDataFactory.CreatePublishedTestAsync(db, now, "UNANS1", 1, questionCount: 5);

        using var client = _factory.CreateClient();
        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, "Nortoshov Sardorbek Muzaffarovich", new DateOnly(2010, 2, 2), Gender.Male, 9, "A",
            "+998901234567", null, null, true, "uz");
        var startSessionResponse = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        startSessionResponse.EnsureSuccessStatusCode();
        var sessionToken = (await startSessionResponse.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!.SessionToken;
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        await client.PostAsync(new Uri("/api/public/sessions/tests/UNANS1/start", UriKind.Relative), content: null);

        var questionIds = testDefinition.Questions.OrderBy(q => q.DisplayOrder).Select(q => q.Id).ToList();

        // 5 tadan faqat 2 tasiga javob — 3 ta javobsiz qoladi.
        var payload = new { answers = questionIds.Take(2).Select(id => new { questionId = id, value = 3, durationMs = 1000 }).ToList() };
        var saveResponse = await client.PostAsJsonAsync("/api/public/sessions/tests/UNANS1/answers", payload, TestJson.Options);
        saveResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsync(new Uri("/api/public/sessions/tests/UNANS1/complete", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        problem.GetProperty("unansweredCount").GetInt32().Should().Be(3);
    }

    /// <summary>
    /// QA tuzatmasi (`prompts/12`): `IsRequired = false` savol javobsiz qolsa `CompleteTest`
    /// baribir muvaffaqiyatli o'tishi shart — avval bu (P33'gacha ko'rinmaydigan) xato
    /// mavjud edi, chunki tekshiruv `TotalCount` (barcha faol savol) ga tayanardi.
    /// </summary>
    [Fact]
    public async Task CompleteTest_FaqatIxtiyoriySavolJavobsizQolganda_MuvaffaqiyatliYakunlanadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("optional1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-optional1", accessToken);
        // RIASEC shaklida: 48 ta majburiy (6 tip x 8) + 2 ta IXTIYORIY ("FILLER") savol —
        // `RIASEC` (SUM'dan farqli) `InterpretationBands`siz haqiqiy scoring bilan yakunlanadi
        // (`TestDataFactory` izohiga qarang), shu sabab bu test faqat domen invariantini emas,
        // TO'LIQ `CompleteTest` oqimini (scoring + `TestResult` yozish) muvaffaqiyatli sinaydi.
        var testDefinition = await TestDataFactory.CreatePublishedRiasecShapedTestWithOptionalExtrasAsync(db, now, "OPT1", 1, extraOptionalCount: 2);

        using var client = _factory.CreateClient();
        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, "Bekova Nodira Sherzodovna", new DateOnly(2010, 7, 7), Gender.Female, 9, "A",
            "+998901234567", null, null, true, "uz");
        var startSessionResponse = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        startSessionResponse.EnsureSuccessStatusCode();
        var sessionToken = (await startSessionResponse.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!.SessionToken;
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        await client.PostAsync(new Uri("/api/public/sessions/tests/OPT1/start", UriKind.Relative), content: null);

        // Faqat MAJBURIY (RIASEC shkalali, "FILLER" bo'lmagan) savollarga javob beramiz —
        // 2 ta ixtiyoriy ("FILLER") savol ATAYLAB javobsiz qoldiriladi.
        var requiredQuestionIds = testDefinition.Questions
            .Where(q => q.IsRequired)
            .OrderBy(q => q.DisplayOrder)
            .Select(q => q.Id)
            .ToList();
        requiredQuestionIds.Should().HaveCount(48);

        var payload = new { answers = requiredQuestionIds.Select(id => new { questionId = id, value = 3, durationMs = 1000 }).ToList() };
        var saveResponse = await client.PostAsJsonAsync("/api/public/sessions/tests/OPT1/answers", payload, TestJson.Options);
        saveResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsync(new Uri("/api/public/sessions/tests/OPT1/complete", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "ixtiyoriy savol javobsiz qolishi yakunlashni bloklamasligi shart");
        var body = await response.Content.ReadFromJsonAsync<CompleteTestResult>(TestJson.Options);
        body!.Status.Should().Be("Completed");
    }

    /// <summary>
    /// `unansweredCount` faqat javobsiz MAJBURIY savollarni sanashi shart — ixtiyoriylarni EMAS
    /// (QA tuzatmasi). 5 savol (3 majburiy, 2 ixtiyoriy), faqat 1 ta majburiy javoblangan:
    /// eski (buzilgan) hisob bo'yicha `unansweredCount` 4 chiqar edi (5-1), to'g'ri hisobda 2
    /// (faqat Q2/Q3 — qolgan ikkita majburiy).
    /// </summary>
    [Fact]
    public async Task CompleteTest_MajburiySavolJavobsizQolganda_UnansweredCountFaqatMajburiylarniSanaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("optional2");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-optional2", accessToken);
        var testDefinition = await TestDataFactory.CreatePublishedTestAsync(db, now, "OPT2", 1, questionCount: 5, requiredCount: 3);

        using var client = _factory.CreateClient();
        var command = new StartSessionCommand(
            school.Slug.Value, accessToken, null, "Qosimov Jahongir Baxtiyorovich", new DateOnly(2010, 8, 8), Gender.Male, 9, "A",
            "+998901234567", null, null, true, "uz");
        var startSessionResponse = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);
        startSessionResponse.EnsureSuccessStatusCode();
        var sessionToken = (await startSessionResponse.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options))!.SessionToken;
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        await client.PostAsync(new Uri("/api/public/sessions/tests/OPT2/start", UriKind.Relative), content: null);

        var questionIds = testDefinition.Questions.OrderBy(q => q.DisplayOrder).Select(q => q.Id).ToList();

        // Faqat 1-savolga (majburiy) javob — 2/3 majburiy va 2/2 ixtiyoriy javobsiz qoladi.
        var payload = new { answers = questionIds.Take(1).Select(id => new { questionId = id, value = 3, durationMs = 1000 }).ToList() };
        var saveResponse = await client.PostAsJsonAsync("/api/public/sessions/tests/OPT2/answers", payload, TestJson.Options);
        saveResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsync(new Uri("/api/public/sessions/tests/OPT2/complete", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        problem.GetProperty("unansweredCount").GetInt32().Should().Be(2, "faqat 2 ta MAJBURIY savol (Q2/Q3) javobsiz — 2 ta ixtiyoriy (Q4/Q5) sanalmasin");
    }
}
