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
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `AssessmentsController.GetAnswers` — `docs/07` 3.3-bo'lim: "Xom javoblar (audit uchun)",
/// `prompts/15` MAXSUS DIQQAT #5. Alohida `IClassFixture`.
/// </summary>
public sealed class AdminAssessmentsAnswersEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminAssessmentsAnswersEndpointTests(PublicApiTestFactory factory)
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
    public async Task GetAnswers_MavjudEmasSessiya_404Qaytaradi()
    {
        using var client = await AuthenticatedClientAsync("assessments-answers-404-admin");

        var response = await client.GetAsync(new Uri($"/api/admin/assessments/{Guid.NewGuid()}/answers", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAnswers_XomJavoblarniQaytaradi_TestCodeFiltriIshlaydiVaScaleYoq()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "assess-answers-a", TestDataFactory.NewAccessToken("assess-answers-a"));
        var testA = await TestDataFactory.CreatePublishedTestAsync(db, now, "ANS-A", 1, questionCount: 2);
        var testB = await TestDataFactory.CreatePublishedTestAsync(db, now, "ANS-B", 2, questionCount: 1);

        var phone = PhoneNumber.Create("+998907773001").Value;
        var student = Student.Create(Guid.NewGuid(), school.Id, "Ergasheva Nodira Bahodirovna", new DateOnly(2009, 3, 3), Gender.Female, 9, phone, now, now);
        db.Students.Add(student);

        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var assessment = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, "assess-answers-session-0123456789ab", "uz", programId, now.AddMinutes(-30), now.AddDays(7), now);
        var assessmentTestA = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testA.Id, 1, totalCount: 2);
        var assessmentTestB = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testB.Id, 2, totalCount: 1);
        assessment.AddTest(assessmentTestA);
        assessment.AddTest(assessmentTestB);

        var questionsA = await db.Questions.AsNoTracking().Where(q => q.TestDefinitionId == testA.Id).OrderBy(q => q.DisplayOrder).ToListAsync();
        var questionB = await db.Questions.AsNoTracking().FirstAsync(q => q.TestDefinitionId == testB.Id);

        assessment.StartTest(testA.Id, now.AddMinutes(-30));
        assessmentTestA.UpsertAnswer(Guid.NewGuid(), questionsA[0].Id, 3, null, 1200, now.AddMinutes(-29));
        assessmentTestA.UpsertAnswer(Guid.NewGuid(), questionsA[1].Id, 5, null, 900, now.AddMinutes(-28));
        // Qayta javob — `revisionCount` oshishini tekshirish uchun.
        assessmentTestA.UpsertAnswer(Guid.NewGuid(), questionsA[1].Id, 4, null, 1500, now.AddMinutes(-27));

        assessment.StartTest(testB.Id, now.AddMinutes(-20));
        assessmentTestB.UpsertAnswer(Guid.NewGuid(), questionB.Id, 2, null, 800, now.AddMinutes(-19));

        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("assessments-answers-admin");

        var all = await client.GetFromJsonAsync<AdminAssessmentAnswersDto>(
            $"/api/admin/assessments/{assessment.Id}/answers", TestJson.Options);
        all!.Answers.Should().HaveCount(3);
        all.Answers.Select(a => a.TestCode).Should().Contain(["ANS-A", "ANS-B"]);

        var revisedAnswer = all.Answers.Single(a => a.QuestionId == questionsA[1].Id);
        revisedAnswer.RawValue.Should().Be(4);
        revisedAnswer.RevisionCount.Should().Be(1);
        revisedAnswer.DurationMs.Should().Be(1500);
        revisedAnswer.QuestionText.Should().NotBeNullOrWhiteSpace();

        var onlyB = await client.GetFromJsonAsync<AdminAssessmentAnswersDto>(
            $"/api/admin/assessments/{assessment.Id}/answers?testCode=ANS-B", TestJson.Options);
        onlyB!.Answers.Should().ContainSingle(a => a.QuestionId == questionB.Id);

        // Signallar filtrga QARAMAY butun sessiya bo'yicha (`AdminAssessmentAnswersDto` izohi):
        // `ReliabilityCalculator` ham sessiya darajasida ishlaydi.
        onlyB.Session.AnsweredCount.Should().Be(3, "signallar `testCode` filtridan qat'i nazar BUTUN sessiya bo'yicha");

        var notAssigned = await client.GetFromJsonAsync<AdminAssessmentAnswersDto>(
            $"/api/admin/assessments/{assessment.Id}/answers?testCode=NOPE", TestJson.Options);
        notAssigned!.Answers.Should().BeEmpty();

        // ⚠️ 2026-09-03 dan buyon `scale`/`scaleDirection`/`scaleNameUz`/`effectiveValue`
        // ADMIN javobida ATAYLAB BOR (egasining talabi: javobning ma'nosi ko'rinsin) —
        // `CLAUDE.md` 9-bandi faqat O'QUVCHI API'siga tegishli va u
        // `PublicTestQuestionsEndpointTests` da xom JSON + swagger sxemasi ustidan qulflangan.
        var raw = await client.GetStringAsync(new Uri($"/api/admin/assessments/{assessment.Id}/answers", UriKind.Relative));
        raw.Should().Contain("\"scaleDirection\"");
        raw.Should().Contain("\"effectiveValue\"");
    }

    /// <summary>
    /// Egasi topgan kamchilik (2026-09-12): `Survey` (so'rovnoma) javoblari — `MultiChoice`
    /// variant matnlari, matn javoblari, va Likert semantikasiga oid maydonlarning `null`
    /// bo'lishi (nol/soxta `false` EMAS). Haqiqiy DB (EF Core) orqali — real proyeksiya va
    /// jsonb serializatsiya to'g'ri ishlashini tekshiradi.
    /// </summary>
    [Fact]
    public async Task GetAnswers_SorovnomaJavoblari_VariantMatniVaMatnJavobiToLiqQaytadiVaLikertMaydonlariNullBoLadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var scoredTest = await TestDataFactory.CreatePublishedTestAsync(db, now, "SRV-SCORED", 1, questionCount: 2);

        var surveyTestId = Guid.NewGuid();
        var surveyTest = TestDefinition.Create(
            surveyTestId, "SRV-SURVEY", "So'rovnoma bloki", displayOrder: 2, estimatedMinutes: 3,
            scoringStrategyCode: null, now: now, scoringMode: TestScoringMode.Survey);
        var multiChoiceQuestion = Question.Create(
            Guid.NewGuid(), surveyTestId, "SRV-Q1", 1, "Qaysi fanlarga qiziqasiz?",
            QuestionType.MultiChoice, "SURVEY", scaleDirection: 1, weight: 1.0m);
        var textQuestion = Question.Create(
            Guid.NewGuid(), surveyTestId, "SRV-Q2", 2, "Maktab va sinf",
            QuestionType.ShortText, "SURVEY", scaleDirection: 1, weight: 1.0m);
        surveyTest.AddQuestion(multiChoiceQuestion, now);
        surveyTest.AddQuestion(textQuestion, now);
        surveyTest.Publish(now);
        multiChoiceQuestion.AddOption(AnswerOption.Create(Guid.NewGuid(), multiChoiceQuestion.Id, "Ingliz tili", 1, 1));
        multiChoiceQuestion.AddOption(AnswerOption.Create(Guid.NewGuid(), multiChoiceQuestion.Id, "Nemis tili", 2, 2));
        multiChoiceQuestion.AddOption(AnswerOption.Create(Guid.NewGuid(), multiChoiceQuestion.Id, "Matematika", 3, 3));
        db.TestDefinitions.Add(surveyTest);

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "assess-answers-survey", TestDataFactory.NewAccessToken("assess-answers-survey"));
        var phone = PhoneNumber.Create("+998907773002").Value;
        var student = Student.Create(Guid.NewGuid(), school.Id, "Yusupova Malika Alisherovna", new DateOnly(2008, 5, 5), Gender.Female, 10, phone, now, now);
        db.Students.Add(student);

        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var assessment = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, "assess-answers-survey-session-0123456789ab", "uz", programId, now.AddMinutes(-30), now.AddDays(7), now);
        var assessmentTestScored = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, scoredTest.Id, 1, totalCount: 2);
        var assessmentTestSurvey = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, surveyTestId, 2, totalCount: 2);
        assessment.AddTest(assessmentTestScored);
        assessment.AddTest(assessmentTestSurvey);

        var scoredQuestions = await db.Questions.AsNoTracking().Where(q => q.TestDefinitionId == scoredTest.Id).OrderBy(q => q.DisplayOrder).ToListAsync();

        assessment.StartTest(scoredTest.Id, now.AddMinutes(-30));
        assessmentTestScored.UpsertAnswer(Guid.NewGuid(), scoredQuestions[0].Id, 4, null, 1200, now.AddMinutes(-29));
        assessmentTestScored.UpsertAnswer(Guid.NewGuid(), scoredQuestions[1].Id, 3, null, 1300, now.AddMinutes(-28));

        assessment.StartTest(surveyTestId, now.AddMinutes(-20));
        // Saqlangan tartib ATAYLAB variantlar ro'yxati tartibidan (1,2,3) farqli — 3 keyin 1.
        assessmentTestSurvey.UpsertAnswer(Guid.NewGuid(), multiChoiceQuestion.Id, null, null, 4200, now.AddMinutes(-19), selectedValues: [3, 1]);
        assessmentTestSurvey.UpsertAnswer(Guid.NewGuid(), textQuestion.Id, null, null, 6000, now.AddMinutes(-18), textValue: "45-maktab, 9-sinf");

        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("assessments-answers-survey-admin");

        var response = await client.GetFromJsonAsync<AdminAssessmentAnswersDto>(
            $"/api/admin/assessments/{assessment.Id}/answers", TestJson.Options);

        response!.Answers.Should().HaveCount(4);

        var multiChoiceRow = response.Answers.Single(a => a.QuestionId == multiChoiceQuestion.Id);
        multiChoiceRow.ScoringMode.Should().Be("Survey");
        multiChoiceRow.SelectedValues.Should().Equal(3, 1);
        multiChoiceRow.SelectedOptionTexts.Should().Equal("Matematika", "Ingliz tili");
        multiChoiceRow.RawValue.Should().BeNull();
        // Likert semantikasiga oid maydonlar — `Survey` qatorda `null` ("qo'llanilmaydi"),
        // `0`/`false` EMAS (egasi topgan kamchilik).
        multiChoiceRow.EffectiveValue.Should().BeNull();
        multiChoiceRow.IsFastAnswer.Should().BeNull();

        var textRow = response.Answers.Single(a => a.QuestionId == textQuestion.Id);
        textRow.ScoringMode.Should().Be("Survey");
        textRow.TextValue.Should().Be("45-maktab, 9-sinf");
        textRow.SelectedOptionTexts.Should().BeNull();
        textRow.EffectiveValue.Should().BeNull();
        textRow.IsFastAnswer.Should().BeNull();

        var scoredRows = response.Answers.Where(a => a.TestCode == "SRV-SCORED").ToList();
        scoredRows.Should().HaveCount(2);
        scoredRows.Should().OnlyContain(a => a.ScoringMode == "Scored");
        scoredRows.Should().OnlyContain(a => a.EffectiveValue != null && a.IsFastAnswer != null);

        // `docs/03` §7: `Survey` bloklari ishonchlilik hisobiga KIRMAYDI — sessiya signali
        // faqat 2 ta `Scored` javobni sanaydi (4 EMAS).
        response.Session.AnsweredCount.Should().Be(2, "`Survey` javoblari ishonchlilik hisobiga kirmaydi");

        // XOM JSON — `effectiveValue`/`isFastAnswer` `Survey` qatorlarda `0`/`false` EMAS,
        // `null` bo'lishi harfma-harf tekshiriladi (`ReadFromJsonAsync` kalit farqini ko'rmaydi).
        var raw = await client.GetStringAsync(new Uri($"/api/admin/assessments/{assessment.Id}/answers", UriKind.Relative));
        raw.Should().Contain("\"selectedOptionTexts\":[\"Matematika\",\"Ingliz tili\"]");
        raw.Should().Contain("\"scoringMode\":\"Survey\"");
        raw.Should().Contain("\"scoringMode\":\"Scored\"");
    }
}
