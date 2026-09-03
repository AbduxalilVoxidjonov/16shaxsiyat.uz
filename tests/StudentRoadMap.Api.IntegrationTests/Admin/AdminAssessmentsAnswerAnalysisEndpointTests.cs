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
using StudentRoadMap.Domain.Scoring;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `GET /api/admin/assessments/{id}/answers` — SAVOLMA-SAVOL TAHLIL (`docs/07` §3.3).
/// Egasining talabi (2026-09-03): profil sahifasida har savolga berilgan javob va uning
/// ma'nosi ko'rinsin. Bu yerda tekshiriladigan eng muhim narsa — `effectiveValue`:
/// teskari savolga (`scaleDirection = -1`) berilgan `5` shkalaga `1` bo'lib tushadi
/// (`docs/03` §1: `v' = (max + min) − v`). Xom `5` ni ko'rsatish psixologni BUTUNLAY
/// teskari xulosaga olib boradi.
/// </summary>
public sealed class AdminAssessmentsAnswerAnalysisEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminAssessmentsAnswerAnalysisEndpointTests(PublicApiTestFactory factory)
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

    /// <summary>Bitta `RIASEC` strategiyali anketa — shkala nomlari `SystemScaleCatalog`dan keladi.</summary>
    private sealed record QuestionSpec(string Scale, int Direction, int Value, int DurationMs);

    private static async Task<(Guid AssessmentId, IReadOnlyList<Guid> QuestionIds)> SeedAsync(
        AppDbContext db,
        DateTimeOffset now,
        string slug,
        string testCode,
        string phone,
        IReadOnlyList<QuestionSpec> specs,
        int sessionDurationSeconds)
    {
        var school = await TestDataFactory.CreateSchoolAsync(db, now, slug, TestDataFactory.NewAccessToken(slug));

        var testId = Guid.NewGuid();
        var test = TestDefinition.Create(
            testId,
            testCode,
            $"{testCode} nomi",
            displayOrder: 1,
            estimatedMinutes: 5,
            scoringStrategyCode: "RIASEC",
            now: now);

        var questionIds = new List<Guid>();
        for (var i = 0; i < specs.Count; i++)
        {
            var questionId = Guid.NewGuid();
            questionIds.Add(questionId);
            test.AddQuestion(
                Question.Create(
                    questionId,
                    testId,
                    $"{testCode}-Q{i + 1:00}",
                    i + 1,
                    $"{testCode} savoli {i + 1}",
                    QuestionType.Likert5,
                    specs[i].Scale,
                    scaleDirection: specs[i].Direction,
                    weight: 1.0m,
                    isRequired: true),
                now);
        }

        test.Publish(now);
        db.TestDefinitions.Add(test);
        await db.SaveChangesAsync();

        var studentPhone = PhoneNumber.Create(phone).Value;
        var student = Student.Create(
            Guid.NewGuid(), school.Id, "Ergasheva Nodira Bahodirovna", new DateOnly(2009, 3, 3),
            Gender.Female, 9, studentPhone, now, now);
        db.Students.Add(student);

        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var assessment = Assessment.Create(
            Guid.NewGuid(), student.Id, school.Id, $"{slug}-session-0123456789ab", "uz",
            programId, now.AddMinutes(-40), now.AddDays(7), now);
        var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testId, 1, totalCount: specs.Count);
        assessment.AddTest(assessmentTest);
        assessment.StartTest(testId, now.AddMinutes(-40));

        for (var i = 0; i < specs.Count; i++)
        {
            assessmentTest.UpsertAnswer(Guid.NewGuid(), questionIds[i], specs[i].Value, null, specs[i].DurationMs, now.AddMinutes(-39));
        }

        db.Assessments.Add(assessment);

        // `TotalDurationSeconds` — `ShortSession` signalining YAGONA manbai (`docs/03` §7,
        // `RecalculateAssessmentScoresCommandHandler` ham aynan shuni o'qiydi). Domen setteri
        // yopiq (`Complete` barcha bloklar tugashini talab qiladi) — sinov ma'lumoti EF
        // orqali to'g'ridan-to'g'ri qo'yiladi.
        db.Entry(assessment).Property(a => a.TotalDurationSeconds).CurrentValue = sessionDurationSeconds;
        await db.SaveChangesAsync();

        return (assessment.Id, questionIds);
    }

    private static JsonElement AnswerAt(JsonDocument document, int index) =>
        document.RootElement.GetProperty("answers")[index];

    /// <summary>
    /// ⚠️ ENG MUHIM TEST. Teskari savol (`scaleDirection = -1`) + javob `5` →
    /// `effectiveValue = 1` (`docs/03` §1). To'g'ri savolda `effectiveValue = rawValue`.
    /// </summary>
    [Fact]
    public async Task GetAnswers_TeskariSavol_EffectiveValueniTuzatibKorsatadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var (assessmentId, _) = await SeedAsync(
            db, now, "ans-eff", "ANS-EFF", "+998907773101",
            [
                new QuestionSpec("ART", -1, 5, 2000),
                new QuestionSpec("ART", +1, 4, 2000),
            ],
            sessionDurationSeconds: 1800);

        using var client = await AuthenticatedClientAsync("assessments-answer-eff-admin");

        var raw = await client.GetStringAsync(new Uri($"/api/admin/assessments/{assessmentId}/answers", UriKind.Relative));
        using var document = JsonDocument.Parse(raw);

        var reverse = AnswerAt(document, 0);
        reverse.GetProperty("rawValue").GetInt32().Should().Be(5);
        reverse.GetProperty("scaleDirection").GetInt32().Should().Be(-1);
        reverse.GetProperty("effectiveValue").GetInt32().Should()
            .Be(1, "teskari savolga berilgan 5 shkalaga 1 bo'lib tushadi (docs/03 §1: v' = 6 − v)");
        reverse.GetProperty("scale").GetString().Should().Be("ART");
        reverse.GetProperty("scaleNameUz").GetString().Should().Be("Artistik");

        var forward = AnswerAt(document, 1);
        forward.GetProperty("effectiveValue").GetInt32().Should().Be(4, "to'g'ri savolda tuzatish qo'llanmaydi");
    }

    /// <summary>
    /// Tez javob chegarasi — `ScoringConstants.FastAnswerDurationThresholdMs` (900 ms).
    /// Chegaraning O'ZI (`900`) tez EMAS: `docs/03` §7 shartida `DurationMs < 900`.
    /// </summary>
    [Fact]
    public async Task GetAnswers_TezJavob_ChegaradanKichigiBelgilanadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var (assessmentId, _) = await SeedAsync(
            db, now, "ans-fast", "ANS-FAST", "+998907773102",
            [
                new QuestionSpec("ART", +1, 4, 899),
                new QuestionSpec("ART", +1, 2, 900),
                new QuestionSpec("SOC", +1, 5, 5000),
            ],
            sessionDurationSeconds: 1800);

        using var client = await AuthenticatedClientAsync("assessments-answer-fast-admin");

        var raw = await client.GetStringAsync(new Uri($"/api/admin/assessments/{assessmentId}/answers", UriKind.Relative));
        using var document = JsonDocument.Parse(raw);

        AnswerAt(document, 0).GetProperty("isFastAnswer").GetBoolean().Should().BeTrue("899 < 900");
        AnswerAt(document, 1).GetProperty("isFastAnswer").GetBoolean().Should().BeFalse("chegaraning o'zi tez emas (900 < 900 yolg'on)");
        AnswerAt(document, 2).GetProperty("isFastAnswer").GetBoolean().Should().BeFalse();

        document.RootElement.GetProperty("thresholds").GetProperty("fastAnswerDurationMs").GetInt32()
            .Should().Be(ScoringConstants.FastAnswerDurationThresholdMs, "chegara backend'dan uzatiladi, frontendda takrorlanmaydi");
    }

    /// <summary>
    /// Straight-lining — ketma-ket ≥ 12 ta bir xil qiymat (`ScoringConstants.StraightLiningMinRunLength`).
    /// To'liq blokdagi javoblar `straightLiningBlockIndex` bilan belgilanadi; blokdan tashqaridagilar `null`.
    /// </summary>
    [Fact]
    public async Task GetAnswers_StraightLiningSeriyasi_AjratibKorsatiladi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var specs = new List<QuestionSpec>();
        for (var i = 0; i < ScoringConstants.StraightLiningMinRunLength; i++)
        {
            specs.Add(new QuestionSpec("ART", +1, 3, 3000));
        }

        specs.Add(new QuestionSpec("SOC", +1, 1, 3000));
        specs.Add(new QuestionSpec("SOC", +1, 5, 3000));

        var (assessmentId, _) = await SeedAsync(
            db, now, "ans-line", "ANS-LINE", "+998907773103", specs, sessionDurationSeconds: 1800);

        using var client = await AuthenticatedClientAsync("assessments-answer-line-admin");

        var raw = await client.GetStringAsync(new Uri($"/api/admin/assessments/{assessmentId}/answers", UriKind.Relative));
        using var document = JsonDocument.Parse(raw);

        for (var i = 0; i < ScoringConstants.StraightLiningMinRunLength; i++)
        {
            AnswerAt(document, i).GetProperty("straightLiningBlockIndex").GetInt32()
                .Should().Be(1, $"{i + 1}-javob 12talik to'liq blok ichida");
        }

        AnswerAt(document, ScoringConstants.StraightLiningMinRunLength).GetProperty("straightLiningBlockIndex")
            .ValueKind.Should().Be(JsonValueKind.Null, "seriya uzilgan — blokdan tashqarida");

        var session = document.RootElement.GetProperty("session");
        session.GetProperty("straightLiningBlockCount").GetInt32().Should().Be(1);
        session.GetProperty("allSameAnswer").GetBoolean().Should().BeFalse("oxirgi ikki javob boshqacha");
        session.GetProperty("shortSession").GetBoolean().Should().BeFalse("30 daqiqa — 6 daqiqalik chegaradan uzun");
    }
}
