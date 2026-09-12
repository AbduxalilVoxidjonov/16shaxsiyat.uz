using FluentAssertions;
using StudentRoadMap.Application.Admin.Assessments.GetAnswers;
using StudentRoadMap.Application.Tests.Admin.Assessments.Testing;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using Xunit;

namespace StudentRoadMap.Application.Tests.Admin.Assessments;

/// <summary>
/// Egasi topgan kamchilik (2026-09-12, `feat/P52-tarmoqlanuvchi-sorovnoma`): admin o'quvchi
/// profilidagi "javoblar" bo'limi so'rovnoma (`ScoringMode = Survey`) javoblarini to'liq
/// ko'rsatmas edi — `MultiChoice` javobida faqat xom `[1, 3]` ko'rinardi, `EffectiveValue`/
/// `IsFastAnswer` esa `Survey` qatorlarda ma'nosiz `0`/`false` edi. Bu testlar shu tuzatishni
/// qulflaydi.
/// </summary>
public sealed class GetAssessmentAnswersQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 8, 0, 0, TimeSpan.Zero);

    private sealed record Fixture(
        FakeAssessmentAnswersAppDbContext Db,
        Assessment Assessment,
        AssessmentTest AssessmentTest,
        TestDefinition TestDefinition);

    /// <summary>
    /// `Survey` (ballanmaydigan) anketa — bitta InProgress sessiyaga biriktirilgan.
    /// `questionFactory` — anketaning haqiqiy `testId`sidan foydalanib savollarni quradi.
    /// </summary>
    private static Fixture CreateSurveyFixture(FakeAssessmentAnswersAppDbContext db, Func<Guid, IReadOnlyList<Question>> questionFactory)
    {
        var testId = Guid.NewGuid();
        var test = TestDefinition.Create(
            testId,
            "SURVEY-" + testId.ToString("N")[..8],
            "So'rovnoma",
            displayOrder: 1,
            estimatedMinutes: 3,
            scoringStrategyCode: null,
            now: Now,
            scoringMode: TestScoringMode.Survey);

        var questions = questionFactory(testId);
        foreach (var question in questions)
        {
            test.AddQuestion(question, Now);
        }

        test.Publish(Now);
        db.TestDefinitionList.Add(test);
        // `_context.Questions` — TEKIS jadval so'rovi (`TestDefinition.Questions` navigatsiyasi
        // EMAS), Fake'da alohida to'ldirilishi kerak.
        db.QuestionList.AddRange(test.Questions);

        var assessment = Assessment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "survey-fixture-session-" + testId.ToString("N")[..12], "uz",
            Guid.NewGuid(), Now.AddMinutes(-10), Now.AddDays(7), Now);
        var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testId, 1, totalCount: Math.Max(questions.Count, 1));
        assessment.AddTest(assessmentTest);
        assessment.StartTest(testId, Now.AddMinutes(-10));

        db.AssessmentList.Add(assessment);
        db.AssessmentTestList.Add(assessmentTest);

        return new Fixture(db, assessment, assessmentTest, test);
    }

    /// <summary>Domen agregatiga yozilgan javoblarni Fake `Answers` jadvaliga ko'chiradi.</summary>
    private static void SyncAnswers(FakeAssessmentAnswersAppDbContext db, AssessmentTest assessmentTest)
    {
        db.AnswerList.RemoveAll(a => a.AssessmentTestId == assessmentTest.Id);
        db.AnswerList.AddRange(assessmentTest.Answers);
    }

    [Fact]
    public async Task Handle_MultiChoiceJavobi_VariantMatnlariniSaqlanganTartibdaQaytaradi()
    {
        var db = new FakeAssessmentAnswersAppDbContext();
        var fixture = CreateSurveyFixture(db, testId =>
        [
            Question.Create(Guid.NewGuid(), testId, "Q_FANLAR", 1, "Qaysi fanlarga qiziqasiz?",
                QuestionType.MultiChoice, "SURVEY", scaleDirection: 1, weight: 1.0m),
        ]);
        var question = fixture.TestDefinition.Questions.Single();

        var optionIngliz = AnswerOption.Create(Guid.NewGuid(), question.Id, "Ingliz tili", 1, 1);
        var optionNemis = AnswerOption.Create(Guid.NewGuid(), question.Id, "Nemis tili", 2, 2);
        var optionMatematika = AnswerOption.Create(Guid.NewGuid(), question.Id, "Matematika", 3, 3);
        db.AnswerOptionList.AddRange([optionIngliz, optionNemis, optionMatematika]);

        // Saqlangan tartib: avval Matematika (3), keyin Ingliz tili (1) — variantlar
        // ro'yxatidagi tartibdan (1,2,3) ATAYLAB farqli, tartib SAQLANISHINI tekshirish uchun.
        fixture.AssessmentTest.UpsertAnswer(Guid.NewGuid(), question.Id, null, null, 4200, Now, selectedValues: [3, 1]);
        SyncAnswers(db, fixture.AssessmentTest);

        var handler = new GetAssessmentAnswersQueryHandler(db, new AssessmentAnswersInlineAsyncQueryExecutor());
        var result = await handler.Handle(new GetAssessmentAnswersQuery(fixture.Assessment.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.Answers.Single();
        row.SelectedValues.Should().Equal(3, 1);
        row.SelectedOptionTexts.Should().Equal("Matematika", "Ingliz tili");
        row.ScoringMode.Should().Be("Survey");
        row.RawValue.Should().BeNull();
        row.TextValue.Should().BeNull();
        row.SelectedOptionText.Should().BeNull();

        // Likert semantikasiga oid maydonlar `Survey` qatorda chalg'ituvchi qiymat bermaydi:
        // `0`/`false` EMAS, `null` — "qo'llanilmaydi".
        row.EffectiveValue.Should().BeNull();
        row.IsFastAnswer.Should().BeNull();
        row.StraightLiningBlockIndex.Should().BeNull();
    }

    [Fact]
    public async Task Handle_OchirilganVariant_YiqilmayZaxiraMatnQaytaradi()
    {
        var db = new FakeAssessmentAnswersAppDbContext();
        var fixture = CreateSurveyFixture(db, testId =>
        [
            Question.Create(Guid.NewGuid(), testId, "Q_FANLAR", 1, "Qaysi fanlarga qiziqasiz?",
                QuestionType.MultiChoice, "SURVEY", scaleDirection: 1, weight: 1.0m),
        ]);
        var question = fixture.TestDefinition.Questions.Single();

        // Faqat qiymat 1 uchun variant mavjud (masalan qiymat 99 keyinchalik o'chirilgan) —
        // javobda ikkalasi ham bor.
        db.AnswerOptionList.Add(AnswerOption.Create(Guid.NewGuid(), question.Id, "Ingliz tili", 1, 1));
        fixture.AssessmentTest.UpsertAnswer(Guid.NewGuid(), question.Id, null, null, 3000, Now, selectedValues: [1, 99]);
        SyncAnswers(db, fixture.AssessmentTest);

        var handler = new GetAssessmentAnswersQueryHandler(db, new AssessmentAnswersInlineAsyncQueryExecutor());
        var result = await handler.Handle(new GetAssessmentAnswersQuery(fixture.Assessment.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.Answers.Single();
        row.SelectedOptionTexts.Should().HaveCount(2);
        row.SelectedOptionTexts![0].Should().Be("Ingliz tili");
        row.SelectedOptionTexts![1].Should().Contain("99");
    }

    [Theory]
    [InlineData(QuestionType.ShortText)]
    [InlineData(QuestionType.LongText)]
    [InlineData(QuestionType.Phone)]
    public async Task Handle_MatnJavobi_TextValueBilanQaytadi(QuestionType questionType)
    {
        var db = new FakeAssessmentAnswersAppDbContext();
        var fixture = CreateSurveyFixture(db, testId =>
        [
            Question.Create(Guid.NewGuid(), testId, "Q_MATN", 1, "Matn savoli",
                questionType, "SURVEY", scaleDirection: 1, weight: 1.0m),
        ]);
        var question = fixture.TestDefinition.Questions.Single();

        const string text = "Toshkent shahri, 45-maktab";
        fixture.AssessmentTest.UpsertAnswer(Guid.NewGuid(), question.Id, null, null, 5000, Now, textValue: text);
        SyncAnswers(db, fixture.AssessmentTest);

        var handler = new GetAssessmentAnswersQueryHandler(db, new AssessmentAnswersInlineAsyncQueryExecutor());
        var result = await handler.Handle(new GetAssessmentAnswersQuery(fixture.Assessment.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.Answers.Single();
        row.TextValue.Should().Be(text);
        row.RawValue.Should().BeNull();
        row.SelectedValues.Should().BeNull();
        row.SelectedOptionTexts.Should().BeNull();
        row.ScoringMode.Should().Be("Survey");
        row.EffectiveValue.Should().BeNull();
        row.IsFastAnswer.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ScoredQator_LikertMaydonlariHamonToliq()
    {
        var db = new FakeAssessmentAnswersAppDbContext();
        var testId = Guid.NewGuid();
        var test = TestDefinition.Create(
            testId, "SCORED-TEST", "Ballanadigan test", displayOrder: 1, estimatedMinutes: 3,
            scoringStrategyCode: "SUM", now: Now);
        var question = Question.Create(Guid.NewGuid(), testId, "Q1", 1, "Savol 1", QuestionType.Likert5, "GEN", scaleDirection: 1, weight: 1.0m);
        test.AddQuestion(question, Now);
        test.Publish(Now);
        db.TestDefinitionList.Add(test);
        db.QuestionList.Add(question);

        var assessment = Assessment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "scored-fixture-session-0123456789ab", "uz",
            Guid.NewGuid(), Now.AddMinutes(-10), Now.AddDays(7), Now);
        var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testId, 1, totalCount: 1);
        assessment.AddTest(assessmentTest);
        assessment.StartTest(testId, Now.AddMinutes(-10));
        assessmentTest.UpsertAnswer(Guid.NewGuid(), question.Id, 5, null, 500, Now);

        db.AssessmentList.Add(assessment);
        db.AssessmentTestList.Add(assessmentTest);
        db.AnswerList.AddRange(assessmentTest.Answers);

        var handler = new GetAssessmentAnswersQueryHandler(db, new AssessmentAnswersInlineAsyncQueryExecutor());
        var result = await handler.Handle(new GetAssessmentAnswersQuery(assessment.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.Answers.Single();
        row.ScoringMode.Should().Be("Scored");
        row.RawValue.Should().Be(5);
        row.EffectiveValue.Should().NotBeNull();
        row.IsFastAnswer.Should().NotBeNull();
        row.IsFastAnswer!.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MultiChoiceBatch_QoshimchaSorovSoniSavolSoniga_BogliqEmas()
    {
        var callCounts = new List<int>();

        foreach (var questionCount in new[] { 1, 5 })
        {
            var db = new FakeAssessmentAnswersAppDbContext();
            var fixture = CreateSurveyFixture(db, testId => Enumerable.Range(1, questionCount)
                .Select(i => Question.Create(
                    Guid.NewGuid(), testId, $"Q_MULTI_{i}", i, $"Savol {i}",
                    QuestionType.MultiChoice, "SURVEY", scaleDirection: 1, weight: 1.0m))
                .ToList());

            foreach (var question in fixture.TestDefinition.Questions.OrderBy(q => q.DisplayOrder))
            {
                db.AnswerOptionList.Add(AnswerOption.Create(Guid.NewGuid(), question.Id, "Variant A", 1, 1));
                db.AnswerOptionList.Add(AnswerOption.Create(Guid.NewGuid(), question.Id, "Variant B", 2, 2));
                fixture.AssessmentTest.UpsertAnswer(Guid.NewGuid(), question.Id, null, null, 2000, Now, selectedValues: [1, 2]);
            }

            SyncAnswers(db, fixture.AssessmentTest);

            var executor = new CountingAssessmentAnswersInlineAsyncQueryExecutor();
            var handler = new GetAssessmentAnswersQueryHandler(db, executor);
            var result = await handler.Handle(new GetAssessmentAnswersQuery(fixture.Assessment.Id, null), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Answers.Should().HaveCount(questionCount);
            callCounts.Add(executor.ToListAsyncCallCount);
        }

        callCounts.Distinct().Should().ContainSingle("qo'shimcha so'rov soni savollar soniga bog'liq bo'lmasligi kerak (N+1 emas)");
    }
}
