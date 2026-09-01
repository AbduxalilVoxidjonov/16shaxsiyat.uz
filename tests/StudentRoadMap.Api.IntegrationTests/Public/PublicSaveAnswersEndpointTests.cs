using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.GetTestQuestions;
using StudentRoadMap.Application.Public.SaveAnswers;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// `POST /api/public/sessions/tests/{testCode}/answers` — `docs/07` 1.6-bo'lim, `prompts/11`.
/// Diqqat markazida: idempotent upsert (`RevisionCount`), IDOR himoyasi va qiymat validatsiyasi.
/// </summary>
public sealed class PublicSaveAnswersEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicSaveAnswersEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
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

    [Fact]
    public async Task SaveAnswers_YangiJavoblar_SavedCountVaAnsweredQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("a1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-a1", accessToken);
        var test = await TestDataFactory.CreatePublishedTestAsync(db, now, "A1", 1, questionCount: 3);
        var questions = test.Questions.OrderBy(q => q.DisplayOrder).ToList();

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Rasulov Anvar Habibovich", new DateOnly(2010, 1, 11));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);
        await client.PostAsync(new Uri("/api/public/sessions/tests/A1/start", UriKind.Relative), content: null);

        var request = new
        {
            answers = new[]
            {
                new { questionId = questions[0].Id, value = 3, durationMs = 1000 },
                new { questionId = questions[1].Id, value = 5, durationMs = 800 },
            },
        };

        var response = await client.PostAsJsonAsync("/api/public/sessions/tests/A1/answers", request, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SaveAnswersResult>(TestJson.Options);
        body.Should().NotBeNull();
        body!.SavedCount.Should().Be(2);
        body.Answered.Should().Be(2);
        body.Total.Should().Be(3);
    }

    /// <summary>`prompts/11` DoD: bir savolga ikki marta javob → bitta qator, `RevisionCount = 1`.</summary>
    [Fact]
    public async Task SaveAnswers_BirSavolgaIkkiMartaJavob_BittaQatorVaRevisionCount1()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("a2");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-a2", accessToken);
        var test = await TestDataFactory.CreatePublishedTestAsync(db, now, "A2", 1, questionCount: 2);
        var question = test.Questions.OrderBy(q => q.DisplayOrder).First();

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Nazarova Feruza Shuhratovna", new DateOnly(2010, 2, 12));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);
        await client.PostAsync(new Uri("/api/public/sessions/tests/A2/start", UriKind.Relative), content: null);

        var firstRequest = new { answers = new[] { new { questionId = question.Id, value = 2, durationMs = 500 } } };
        var firstResponse = await client.PostAsJsonAsync("/api/public/sessions/tests/A2/answers", firstRequest, TestJson.Options);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = await firstResponse.Content.ReadFromJsonAsync<SaveAnswersResult>(TestJson.Options);
        firstBody!.Answered.Should().Be(1);

        var secondRequest = new { answers = new[] { new { questionId = question.Id, value = 5, durationMs = 700 } } };
        var secondResponse = await client.PostAsJsonAsync("/api/public/sessions/tests/A2/answers", secondRequest, TestJson.Options);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondBody = await secondResponse.Content.ReadFromJsonAsync<SaveAnswersResult>(TestJson.Options);

        // Ikkinchi yozuvda ham `answered` 1 bo'lishi kerak — yangi qator emas, mavjudi yangilandi.
        secondBody!.Answered.Should().Be(1);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var answers = await verifyDb.Answers.Where(a => a.QuestionId == question.Id).ToListAsync();

        answers.Should().ContainSingle("bir savolga ikki marta javob berilsa bitta qator qolishi kerak");
        answers[0].RevisionCount.Should().Be(1);
        answers[0].RawValue.Should().Be(5, "oxirgi yuborilgan qiymat saqlanishi kerak");
    }

    [Fact]
    public async Task SaveAnswers_QiymatOraliqdanTashqarida_400ValidationErrorQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("a3");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-a3", accessToken);
        var test = await TestDataFactory.CreatePublishedTestAsync(db, now, "A3", 1, questionCount: 1);
        var question = test.Questions.First();

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Xolmatov Diyor Rustamovich", new DateOnly(2010, 3, 13));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);
        await client.PostAsync(new Uri("/api/public/sessions/tests/A3/start", UriKind.Relative), content: null);

        // Likert5 uchun 6 — chegaradan tashqarida (1..5).
        var request = new { answers = new[] { new { questionId = question.Id, value = 6, durationMs = 500 } } };
        var response = await client.PostAsJsonAsync("/api/public/sessions/tests/A3/answers", request, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task SaveAnswers_ElliktadanKopJavob_400Qaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("a4");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-a4", accessToken);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "A4", 1, questionCount: 1);

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Ismoilova Gulnoza Alisherovna", new DateOnly(2010, 4, 14));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);
        await client.PostAsync(new Uri("/api/public/sessions/tests/A4/start", UriKind.Relative), content: null);

        // 51 ta (mavjud bo'lmasa ham) javob — validator DB'ga tegmasdan avval sonini rad etadi.
        var request = new
        {
            answers = Enumerable.Range(0, 51)
                .Select(_ => new { questionId = Guid.NewGuid(), value = 3, durationMs = 100 })
                .ToArray(),
        };

        var response = await client.PostAsJsonAsync("/api/public/sessions/tests/A4/answers", request, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task SaveAnswers_TestHaliBoshlanmagan_ConflictQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("a5");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-a5", accessToken);
        var test = await TestDataFactory.CreatePublishedTestAsync(db, now, "A5", 1, questionCount: 1);
        var question = test.Questions.First();

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Qosimova Shahnoza Odilovna", new DateOnly(2010, 5, 15));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);
        // DIQQAT: `/start` chaqirilmagan — test hali `NotStarted`.

        var request = new { answers = new[] { new { questionId = question.Id, value = 3, durationMs = 500 } } };
        var response = await client.PostAsJsonAsync("/api/public/sessions/tests/A5/answers", request, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SaveAnswers_MuddatiOtganSessiya_410Qaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("a6");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-a6", accessToken);
        var test = await TestDataFactory.CreatePublishedTestAsync(db, now, "A6", 1, questionCount: 1);
        var question = test.Questions.First();

        var phone = PhoneNumber.Create("+998907776655").Value;
        var startedAt = now.AddDays(-10);
        var student = Student.Create(Guid.NewGuid(), school.Id, "Ergashev Bobur Anvarovich", new DateOnly(2010, 6, 16), Gender.Male, 9, phone, startedAt, startedAt);
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var expiredToken = $"expired-token-{Guid.NewGuid():N}";
        var assessment = Assessment.Create(
            Guid.NewGuid(), student.Id, school.Id, expiredToken, "uz",
            startedAt, expiresAt: startedAt.AddDays(7), now: startedAt);
        var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, test.Id, 1, 1);
        assessment.AddTest(assessmentTest);
        assessment.StartTest(test.Id, startedAt);
        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Session-Token", expiredToken);

        var request = new { answers = new[] { new { questionId = question.Id, value = 3, durationMs = 500 } } };
        var response = await client.PostAsJsonAsync("/api/public/sessions/tests/A6/answers", request, TestJson.Options);

        // `SessionTokenAuthenticationHandler` muddati o'tgan sessiyani autentifikatsiya
        // bosqichidayoq `410 SESSION_EXPIRED` bilan rad etadi.
        response.StatusCode.Should().Be(HttpStatusCode.Gone);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("SESSION_EXPIRED");
    }

    [Fact]
    public async Task SaveAnswers_YaroqsizToken_401Qaytaradi()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Session-Token", "mavjud-emas-token");

        var request = new { answers = new[] { new { questionId = Guid.NewGuid(), value = 3, durationMs = 500 } } };
        var response = await client.PostAsJsonAsync("/api/public/sessions/tests/ANY/answers", request, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// `prompts/11` MAXSUS DIQQAT #2 (IDOR): B sessiyasining tokeni bilan yozilgan javoblar
    /// FAQAT B'ning o'z yozuvlariga ta'sir qiladi — A sessiyasining ma'lumotlari o'zgarmaydi.
    /// Wire shartnomada (`docs/07` 1.6) `assessmentTestId` UMUMAN yo'q — nishonlangan yozuv
    /// har doim `X-Session-Token`dan topilgan `AssessmentId` + URL'dagi `testCode` orqali
    /// serverda aniqlanadi, mijoz hech qanday ID bera olmaydi.
    /// </summary>
    [Fact]
    public async Task SaveAnswers_BoshqaSessiyaTokeni_OzSessiyasigaTasirQilmaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("idor1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-idor1", accessToken);
        var test = await TestDataFactory.CreatePublishedTestAsync(db, now, "IDOR1", 1, questionCount: 1);
        var question = test.Questions.First();

        using var client = _factory.CreateClient();

        var tokenA = await StartSessionAsync(client, school, accessToken, "Aliyev Sardor Bekzodovich", new DateOnly(2010, 7, 17));
        var tokenB = await StartSessionAsync(client, school, accessToken, "Boboyev Jasur Toshpolatovich", new DateOnly(2010, 8, 18));
        tokenA.Should().NotBe(tokenB);

        using (var clientA = _factory.CreateClient())
        {
            clientA.DefaultRequestHeaders.Add("X-Session-Token", tokenA);
            await clientA.PostAsync(new Uri("/api/public/sessions/tests/IDOR1/start", UriKind.Relative), content: null);
        }

        using var clientB = _factory.CreateClient();
        clientB.DefaultRequestHeaders.Add("X-Session-Token", tokenB);
        await clientB.PostAsync(new Uri("/api/public/sessions/tests/IDOR1/start", UriKind.Relative), content: null);

        var requestB = new { answers = new[] { new { questionId = question.Id, value = 5, durationMs = 500 } } };
        var responseB = await clientB.PostAsJsonAsync("/api/public/sessions/tests/IDOR1/answers", requestB, TestJson.Options);
        responseB.StatusCode.Should().Be(HttpStatusCode.OK);

        // A sessiyasi HALI ham javob bermagan — B'ning yozuvi A'ga sizib chiqmadi.
        using var clientAVerify = _factory.CreateClient();
        clientAVerify.DefaultRequestHeaders.Add("X-Session-Token", tokenA);
        var questionsA = await clientAVerify.GetFromJsonAsync<GetTestQuestionsResult>(
            "/api/public/sessions/tests/IDOR1/questions?page=1", TestJson.Options);

        questionsA!.Questions.Single(q => q.Id == question.Id).CurrentValue.Should().BeNull();

        // DB darajasida ham tekshiramiz: shu savol bo'yicha faqat B'ning `AssessmentTest`iga
        // tegishli BITTA javob mavjud.
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var answersForQuestion = await verifyDb.Answers.Where(a => a.QuestionId == question.Id).ToListAsync();
        answersForQuestion.Should().ContainSingle();
    }

    /// <summary>
    /// PM qarori (2026-09-02): `errors` kalitlari camelCase bo'lishi kerak — shu jumladan ICHMA-ICH/
    /// INDEKSLI yo'llar (`RuleForEach` orqali `Answers[0].QuestionId` kabi). Faqat identifikator
    /// bo'lagi o'giriladi (`answers[0].questionId`), `[0]` indeksi o'zgarmaydi.
    /// </summary>
    [Fact]
    public async Task SaveAnswers_QuestionIdBosh_ErrorsKalitiIndeksliCamelCaseBoladi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("a7");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-a7", accessToken);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "A7", 1, questionCount: 1);

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Yusupov Aziz Davronovich", new DateOnly(2010, 9, 19));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);
        await client.PostAsync(new Uri("/api/public/sessions/tests/A7/start", UriKind.Relative), content: null);

        var request = new { answers = new[] { new { questionId = Guid.Empty, value = 3, durationMs = 500 } } };
        var response = await client.PostAsJsonAsync("/api/public/sessions/tests/A7/answers", request, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = problem.GetProperty("errors");
        errors.TryGetProperty("answers[0].questionId", out _).Should().BeTrue();
        errors.TryGetProperty("Answers[0].QuestionId", out _).Should().BeFalse();
    }
}
