using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.SaveAnswers;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// `POST /api/public/sessions/tests/{testCode}/answers` — qo'shimcha regressiya testlari
/// (QA topilmasi, 2026-09-02: bu senariylar QA tomonidan vaqtincha yozib sinalgan edi, so'ng
/// o'chirilgan — doimiy to'plamda yo'q edi, ya'ni regressiya sezilmay o'tib ketardi,
/// `CLAUDE.md` 10-qoida: "Test yozilmagan biznes mantiq tugallanmagan hisoblanadi").
///
/// ALOHIDA `IClassFixture` (`PublicSaveAnswersEndpointTests`dan alohida) — o'sha fayl
/// allaqachon o'z IP-based `/sessions` limitiga (10/soat, umumiy `IClassFixture`) yaqin;
/// bu yerga qo'shimcha sessiya-ochish chaqiruvlari qo'shilsa mavjud testlar 429 bo'lib
/// qolishi mumkin edi (avval `PublicStartSessionEndpointTests`da xuddi shu sabab bilan
/// sinov muvaffaqiyatsiz bo'lgan va tuzatilgan).
/// </summary>
public sealed class PublicSaveAnswersRegressionTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicSaveAnswersRegressionTests(PublicApiTestFactory factory)
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

    /// <summary>
    /// `Answer.Id` fixup tuzatishini (2026-09-01) aralash paketda qulflaydi: bitta so'rovda
    /// MAVJUD javobni yangilash + 2 ta YANGI javob qo'shish — hammasi bitta `SaveChangesAsync`
    /// chaqiruvida to'g'ri (yangilari INSERT, mavjudi UPDATE) bajarilishi kerak.
    /// </summary>
    [Fact]
    public async Task SaveAnswers_AralashPaket_MavjudniYangilabIkkiYangiQoshadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("r1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-r1", accessToken);
        var test = await TestDataFactory.CreatePublishedTestAsync(db, now, "R1", 1, questionCount: 3);
        var questions = test.Questions.OrderBy(q => q.DisplayOrder).ToList();

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Nurmatov Botir Yusupovich", new DateOnly(2010, 1, 1));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);
        await client.PostAsync(new Uri("/api/public/sessions/tests/R1/start", UriKind.Relative), content: null);

        // 1. Faqat BITTA savolga birinchi marta javob — aralash paketda YANGILANADI.
        var firstRequest = new { answers = new[] { new { questionId = questions[0].Id, value = 2, durationMs = 500 } } };
        var firstResponse = await client.PostAsJsonAsync("/api/public/sessions/tests/R1/answers", firstRequest, TestJson.Options);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Aralash paket: questions[0] YANGILANADI, questions[1]/[2] YANGI.
        var mixedRequest = new
        {
            answers = new[]
            {
                new { questionId = questions[0].Id, value = 5, durationMs = 700 },
                new { questionId = questions[1].Id, value = 3, durationMs = 400 },
                new { questionId = questions[2].Id, value = 1, durationMs = 300 },
            },
        };
        var mixedResponse = await client.PostAsJsonAsync("/api/public/sessions/tests/R1/answers", mixedRequest, TestJson.Options);

        mixedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var mixedBody = await mixedResponse.Content.ReadFromJsonAsync<SaveAnswersResult>(TestJson.Options);
        mixedBody!.SavedCount.Should().Be(3);
        mixedBody.Answered.Should().Be(3, "3 ta noyob savolning barchasiga javob berilgan (1 tasi yangilangan, 2 tasi yangi)");
        mixedBody.Total.Should().Be(3);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var questionIds = questions.Select(q => q.Id).ToList();
        var answers = await verifyDb.Answers.Where(a => questionIds.Contains(a.QuestionId)).ToListAsync();

        answers.Should().HaveCount(3, "3 ta noyob savol uchun aynan 3 ta qator bo'lishi kerak — dublikat INSERT yo'q");
        answers.Single(a => a.QuestionId == questions[0].Id).RevisionCount.Should().Be(1, "bu savol ikkinchi marta yozilgan");
        answers.Single(a => a.QuestionId == questions[0].Id).RawValue.Should().Be(5);
        answers.Single(a => a.QuestionId == questions[1].Id).RevisionCount.Should().Be(0);
        answers.Single(a => a.QuestionId == questions[2].Id).RevisionCount.Should().Be(0);
    }

    [Fact]
    public async Task SaveAnswers_UchinchiMartaJavob_RevisionCount2Boladi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("r2");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-r2", accessToken);
        var test = await TestDataFactory.CreatePublishedTestAsync(db, now, "R2", 1, questionCount: 1);
        var question = test.Questions.First();

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Odilova Sevinch Baxromovna", new DateOnly(2010, 2, 2));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);
        await client.PostAsync(new Uri("/api/public/sessions/tests/R2/start", UriKind.Relative), content: null);

        foreach (var value in new[] { 2, 4, 5 })
        {
            var request = new { answers = new[] { new { questionId = question.Id, value, durationMs = 200 } } };
            var response = await client.PostAsJsonAsync("/api/public/sessions/tests/R2/answers", request, TestJson.Options);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var answer = await verifyDb.Answers.SingleAsync(a => a.QuestionId == question.Id);

        answer.RevisionCount.Should().Be(2, "uchinchi marta yozilgan javob uchun RevisionCount 2 bo'lishi kerak (birinchisi 0dan boshlanadi)");
        answer.RawValue.Should().Be(5);
    }

    [Fact]
    public async Task SaveAnswers_Aynan50Javob_200Qaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("r3");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-r3", accessToken);
        var test = await TestDataFactory.CreatePublishedTestAsync(db, now, "R3", 1, questionCount: 50);
        var questions = test.Questions.OrderBy(q => q.DisplayOrder).ToList();

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Ravshanov Jahongir Anvarovich", new DateOnly(2010, 3, 3));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);
        await client.PostAsync(new Uri("/api/public/sessions/tests/R3/start", UriKind.Relative), content: null);

        var answerItems = questions.Select(q => new { questionId = q.Id, value = 3, durationMs = 100 }).ToArray();
        answerItems.Should().HaveCount(50);
        var request = new { answers = answerItems };

        var response = await client.PostAsJsonAsync("/api/public/sessions/tests/R3/answers", request, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "aynan 50 (limit ICHIDA) javob qabul qilinishi kerak");
        var body = await response.Content.ReadFromJsonAsync<SaveAnswersResult>(TestJson.Options);
        body!.SavedCount.Should().Be(50);
        body.Answered.Should().Be(50);
        body.Total.Should().Be(50);
    }

    [Fact]
    public async Task SaveAnswers_Likert5ChegaraQiymatlari_0Rad1QabulQilinadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("r4");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-r4", accessToken);
        var test = await TestDataFactory.CreatePublishedTestAsync(db, now, "R4", 1, questionCount: 1);
        var question = test.Questions.First();

        using var client = _factory.CreateClient();
        var sessionToken = await StartSessionAsync(client, school, accessToken, "Tojiboyeva Malika Sunnatovna", new DateOnly(2010, 4, 4));
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);
        await client.PostAsync(new Uri("/api/public/sessions/tests/R4/start", UriKind.Relative), content: null);

        var zeroRequest = new { answers = new[] { new { questionId = question.Id, value = 0, durationMs = 100 } } };
        var zeroResponse = await client.PostAsJsonAsync("/api/public/sessions/tests/R4/answers", zeroRequest, TestJson.Options);
        zeroResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Likert5 uchun 0 chegaradan tashqarida (1..5)");
        var zeroProblem = await zeroResponse.Content.ReadFromJsonAsync<JsonElement>();
        zeroProblem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");

        var oneRequest = new { answers = new[] { new { questionId = question.Id, value = 1, durationMs = 100 } } };
        var oneResponse = await client.PostAsJsonAsync("/api/public/sessions/tests/R4/answers", oneRequest, TestJson.Options);
        oneResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Likert5 uchun 1 — quyi chegara, qabul qilinishi kerak");
    }

    /// <summary>Allaqachon `Completed` bo'lgan `AssessmentTest`ga yozish domen qo'riqchisi tomonidan rad etiladi (`409`).</summary>
    [Fact]
    public async Task SaveAnswers_YakunlanganTestgaYozish_409Qaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("r5");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-r5", accessToken);
        var test = await TestDataFactory.CreatePublishedTestAsync(db, now, "R5", 1, questionCount: 1);
        var question = test.Questions.First();

        var phone = PhoneNumber.Create("+998904445566").Value;
        var student = Student.Create(Guid.NewGuid(), school.Id, "Sattorov Ulug'bek Farhodovich", new DateOnly(2010, 5, 5), Gender.Male, 9, phone, now, now);
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var sessionToken = $"completed-token-{Guid.NewGuid():N}";
        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var assessment = Assessment.Create(
            Guid.NewGuid(), student.Id, school.Id, sessionToken, "uz", programId,
            startedAt: now, expiresAt: now.AddDays(7), now: now);
        var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, test.Id, 1, 1);
        assessment.AddTest(assessmentTest);
        assessment.StartTest(test.Id, now);
        assessmentTest.UpsertAnswer(Guid.NewGuid(), question.Id, 3, null, 100, now);
        assessmentTest.Complete(now, [question.Id]);

        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

        var request = new { answers = new[] { new { questionId = question.Id, value = 5, durationMs = 100 } } };
        var response = await client.PostAsJsonAsync("/api/public/sessions/tests/R5/answers", request, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("ASSESSMENT_TEST_NOT_IN_PROGRESS");
    }
}
