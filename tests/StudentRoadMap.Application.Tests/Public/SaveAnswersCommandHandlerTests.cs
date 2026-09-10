using FluentAssertions;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Application.Public.SaveAnswers;
using StudentRoadMap.Application.Tests.Admin.Ai.Testing;
using StudentRoadMap.Application.Tests.Public.Testing;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Catalog.Branching;

namespace StudentRoadMap.Application.Tests.Public;

/// <summary>
/// `SaveAnswersCommandHandler` — `docs/18` §4.2: 4 ta yangi savol turi (`ShortText`/`LongText`/
/// `Phone`/`MultiChoice`) uchun mazmun tekshiruvi va `QUESTION_NOT_VISIBLE` qo'riqchisi.
/// </summary>
public sealed class SaveAnswersCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ShortTextBosh_ValidationErrorQaytaradi()
    {
        var world = new World();
        var result = await world.SaveAsync(new SaveAnswerItem(world.ShortTextQuestion.Id, null, 500, Text: "   "));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProblemCodes.ValidationError);
    }

    [Fact]
    public async Task Handle_ShortTextUzunlikdanOshsa_ValidationErrorQaytaradi()
    {
        var world = new World();
        var tooLong = new string('a', 300); // MaxLength standart 200
        var result = await world.SaveAsync(new SaveAnswerItem(world.ShortTextQuestion.Id, null, 500, Text: tooLong));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProblemCodes.ValidationError);
    }

    [Fact]
    public async Task Handle_PhoneInputPatternMosEmas_ValidationErrorQaytaradi()
    {
        var world = new World();
        var result = await world.SaveAsync(new SaveAnswerItem(world.PhoneQuestion.Id, null, 500, Text: "not-a-phone"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProblemCodes.ValidationError);
    }

    [Fact]
    public async Task Handle_PhoneToGriShablon_Muvaffaqiyatli()
    {
        var world = new World();
        var result = await world.SaveAsync(new SaveAnswerItem(world.PhoneQuestion.Id, null, 500, Text: "+998901234567"));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_LongTextBosh_ValidationErrorQaytaradi()
    {
        var world = new World();
        var result = await world.SaveAsync(new SaveAnswerItem(world.LongTextQuestion.Id, null, 500, Text: ""));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProblemCodes.ValidationError);
    }

    [Fact]
    public async Task Handle_MultiChoiceBoshTanlov_ValidationErrorQaytaradi()
    {
        var world = new World();
        var result = await world.SaveAsync(new SaveAnswerItem(world.MultiChoiceQuestion.Id, null, 500, SelectedValues: []));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProblemCodes.ValidationError);
    }

    [Fact]
    public async Task Handle_MultiChoiceTakrorlanganTanlov_ValidationErrorQaytaradi()
    {
        var world = new World();
        var result = await world.SaveAsync(new SaveAnswerItem(world.MultiChoiceQuestion.Id, null, 500, SelectedValues: [1, 1]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProblemCodes.ValidationError);
    }

    [Fact]
    public async Task Handle_MultiChoiceNotogriQiymat_ValidationErrorQaytaradi()
    {
        var world = new World();
        var result = await world.SaveAsync(new SaveAnswerItem(world.MultiChoiceQuestion.Id, null, 500, SelectedValues: [999]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProblemCodes.ValidationError);
    }

    [Fact]
    public async Task Handle_MultiChoiceToGriTanlov_Muvaffaqiyatli()
    {
        var world = new World();
        var result = await world.SaveAsync(new SaveAnswerItem(world.MultiChoiceQuestion.Id, null, 500, SelectedValues: [1, 2]));

        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>Shakl invarianti: bir vaqtda ikkita maydon to'ldirilsa (`value` + `text`) — `ANSWER_SHAPE_INVALID`.</summary>
    [Fact]
    public async Task Handle_IkkiXilShaklBirVaqtda_AnswerShapeInvalidQaytaradi()
    {
        var world = new World();
        var result = await world.SaveAsync(new SaveAnswerItem(world.ShortTextQuestion.Id, 1, 500, Text: "matn"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProblemCodes.AnswerShapeInvalid);
    }

    /// <summary>Savol turiga mos kelmagan maydon (Likert savoliga `text` yuborilsa) — `ANSWER_SHAPE_INVALID`.</summary>
    [Fact]
    public async Task Handle_NotogriMaydonShakli_AnswerShapeInvalidQaytaradi()
    {
        var world = new World();
        var result = await world.SaveAsync(new SaveAnswerItem(world.LikertQuestion.Id, null, 500, Text: "matn"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProblemCodes.AnswerShapeInvalid);
    }

    /// <summary>
    /// `docs/18` §4.2: 2-savol (`FilterFollowUp`) faqat 1-savolga (`filterQuestion`) "1" javob
    /// berilsa ko'rinadi. Filtr savoliga hali javob berilmasdan turib 2-savolga yozishga urinish
    /// `400 QUESTION_NOT_VISIBLE` bilan rad etiladi.
    /// </summary>
    [Fact]
    public async Task Handle_KorinmaydiganSavolgaYozish_QuestionNotVisibleQaytaradi()
    {
        var world = new World();
        var result = await world.SaveAsync(new SaveAnswerItem(world.FollowUpQuestion.Id, null, 500, Text: "biror narsa"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProblemCodes.QuestionNotVisible);
    }

    /// <summary>Filtr savoliga to'g'ri javob (SHU so'rov ichida) berilsa — bog'liq savolga yozish RUXSAT etiladi.</summary>
    [Fact]
    public async Task Handle_FiltrSavoligaShuSorovdaJavobBerilsa_BoglikSavolgaYozishRuxsatEtiladi()
    {
        var world = new World();
        var result = await world.Handler.Handle(
            new SaveAnswersCommand(world.Assessment.Id, world.TestCode,
            [
                new SaveAnswerItem(world.FilterQuestion.Id, 1, 400),
                new SaveAnswerItem(world.FollowUpQuestion.Id, null, 500, Text: "endi ko'rinadi"),
            ]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SavedCount.Should().Be(2);
    }

    private sealed class World
    {
        public World()
        {
            var testId = Guid.NewGuid();
            var testDefinition = TestDefinition.Create(
                testId, "SVY-TYPES", "Turlar sinovi", displayOrder: 1, estimatedMinutes: 5,
                scoringStrategyCode: null, now: Now, scoringMode: TestScoringMode.Survey, pageSize: 60);

            ShortTextQuestion = Question.Create(
                Guid.NewGuid(), testId, "Q_SHORT", 1, "Ismingiz?", QuestionType.ShortText,
                scale: "SURVEY", scaleDirection: 1, weight: 1.0m, isRequired: true);
            testDefinition.AddQuestion(ShortTextQuestion, Now);

            LongTextQuestion = Question.Create(
                Guid.NewGuid(), testId, "Q_LONG", 2, "Fikringiz?", QuestionType.LongText,
                scale: "SURVEY", scaleDirection: 1, weight: 1.0m, isRequired: true);
            testDefinition.AddQuestion(LongTextQuestion, Now);

            PhoneQuestion = Question.Create(
                Guid.NewGuid(), testId, "Q_PHONE", 3, "Telefon raqamingiz?", QuestionType.Phone,
                scale: "SURVEY", scaleDirection: 1, weight: 1.0m, isRequired: true,
                inputPattern: "^\\+?998[0-9]{9}$");
            testDefinition.AddQuestion(PhoneQuestion, Now);

            MultiChoiceQuestion = Question.Create(
                Guid.NewGuid(), testId, "Q_MULTI", 4, "Qaysi kurslar qiziq?", QuestionType.MultiChoice,
                scale: "SURVEY", scaleDirection: 1, weight: 1.0m, isRequired: true);
            MultiChoiceQuestion.AddOption(AnswerOption.Create(Guid.NewGuid(), MultiChoiceQuestion.Id, "Matematika", 1, 1));
            MultiChoiceQuestion.AddOption(AnswerOption.Create(Guid.NewGuid(), MultiChoiceQuestion.Id, "Fizika", 2, 2));
            MultiChoiceQuestion.AddOption(AnswerOption.Create(Guid.NewGuid(), MultiChoiceQuestion.Id, "Kimyo", 3, 3));
            testDefinition.AddQuestion(MultiChoiceQuestion, Now);

            LikertQuestion = Question.Create(
                Guid.NewGuid(), testId, "Q_LIKERT", 5, "Roziman", QuestionType.Likert5,
                scale: "SURVEY", scaleDirection: 1, weight: 1.0m, isRequired: false);
            testDefinition.AddQuestion(LikertQuestion, Now);

            // Filtr savoli + shu javobga bog'liq (docs/18 §2.4) kaskad savoli.
            FilterQuestion = Question.Create(
                Guid.NewGuid(), testId, "Q_FILTER", 6, "Qo'shimcha kursga qatnashasizmi?", QuestionType.SingleChoice,
                scale: "SURVEY", scaleDirection: 1, weight: 1.0m, isRequired: true);
            FilterQuestion.AddOption(AnswerOption.Create(Guid.NewGuid(), FilterQuestion.Id, "Ha", 1, 1));
            FilterQuestion.AddOption(AnswerOption.Create(Guid.NewGuid(), FilterQuestion.Id, "Yo'q", 2, 2));
            testDefinition.AddQuestion(FilterQuestion, Now);

            var visibility = new VisibilityRule(
                VisibilityMatch.All,
                [new VisibilityCondition("Q_FILTER", VisibilityOperator.Equals, [1])]);

            FollowUpQuestion = Question.Create(
                Guid.NewGuid(), testId, "Q_FOLLOWUP", 7, "Qaysi kurs?", QuestionType.ShortText,
                scale: "SURVEY", scaleDirection: 1, weight: 1.0m, isRequired: true,
                visibilityRule: visibility);
            testDefinition.AddQuestion(FollowUpQuestion, Now);

            testDefinition.Publish(Now);

            Assessment = Assessment.Create(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "sessiya-tokeni-types", "uz", Guid.NewGuid(),
                Now.AddMinutes(-10), Now.AddDays(7), Now.AddMinutes(-10));

            var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), Assessment.Id, testId, displayOrder: 1, totalCount: 7);
            Assessment.AddTest(assessmentTest);
            Assessment.StartTest(testId, Now.AddMinutes(-10));

            Context = new FakeBranchingSurveyAppDbContext();
            Context.TestDefinitionList.Add(testDefinition);
            Context.QuestionList.AddRange(testDefinition.Questions);
            Context.AnswerOptionList.AddRange(testDefinition.Questions.SelectMany(q => q.Options));
            Context.AssessmentList.Add(Assessment);
            Context.AssessmentTestList.Add(assessmentTest);

            var executor = new InlineAsyncQueryExecutor();
            var cache = new PublicCatalogCache(Context, executor, new InMemoryCacheService());

            Handler = new SaveAnswersCommandHandler(Context, executor, new FakeDateTime(Now), cache);
            TestCode = testDefinition.Code;
        }

        public Assessment Assessment { get; }

        public FakeBranchingSurveyAppDbContext Context { get; }

        public SaveAnswersCommandHandler Handler { get; }

        public string TestCode { get; }

        public Question ShortTextQuestion { get; }

        public Question LongTextQuestion { get; }

        public Question PhoneQuestion { get; }

        public Question MultiChoiceQuestion { get; }

        public Question LikertQuestion { get; }

        public Question FilterQuestion { get; }

        public Question FollowUpQuestion { get; }

        public Task<Domain.Common.Result<SaveAnswersResult>> SaveAsync(SaveAnswerItem item) =>
            Handler.Handle(new SaveAnswersCommand(Assessment.Id, TestCode, [item]), CancellationToken.None);
    }
}
