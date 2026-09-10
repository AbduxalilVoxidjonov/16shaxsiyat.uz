using FluentAssertions;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Application.Public.CompleteTest;
using StudentRoadMap.Application.Tests.Admin.Ai.Testing;
using StudentRoadMap.Application.Tests.Public.Testing;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Catalog.Branching;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Application.Tests.Public;

/// <summary>
/// `CompleteTestCommandHandler` — `docs/18` §4.3: majburiy savol tekshiruvi FAQAT ko'rinadigan
/// savollar bo'yicha (yashirilgan majburiy savol yakunlashni BLOKLAMAYDI) va yashirilgan
/// savollarning javoblari `AssessmentTest.RemoveAnswers` bilan bazadan o'chiriladi.
/// </summary>
public sealed class CompleteTestHiddenRequiredTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_YashirilganMajburiySavolJavobsiz_YakunlashBloklanmaydiVaJavobOchiriladi()
    {
        var world = new World();

        // Filtr savoliga "ko'rsatilmasin" (2) javobi beriladi — `FollowUpQuestion` (majburiy)
        // ko'rinmay qoladi, lekin bazada UNDAN OLDIN yozilgan "eski" javobi bor (o'quvchi
        // avval 1ni tanlab FollowUp'ga javob bergan, keyin fikrini o'zgartirgan ssenariysi).
        world.AssessmentTest.UpsertAnswer(Guid.NewGuid(), world.FollowUpQuestion.Id, null, null, 300, Now.AddMinutes(-5), textValue: "eski javob");
        world.AssessmentTest.UpsertAnswer(Guid.NewGuid(), world.FilterQuestion.Id, 2, world.FilterOptionNo.Id, 300, Now.AddMinutes(-4));

        var answeredBeforeComplete = world.AssessmentTest.AnsweredCount;
        answeredBeforeComplete.Should().Be(2);

        var result = await world.Handler.Handle(new CompleteTestCommand(world.Assessment.Id, world.TestCode), CancellationToken.None);

        result.IsSuccess.Should().BeTrue("yashirilgan majburiy savol (FollowUp) yakunlashni bloklamasligi kerak");
        world.AssessmentTest.Answers.Should().NotContain(a => a.QuestionId == world.FollowUpQuestion.Id, "yashirilgan savolning eski javobi o'chirilishi kerak (docs/18 §2.7/§4.3)");
        world.AssessmentTest.Answers.Should().Contain(a => a.QuestionId == world.FilterQuestion.Id);
        world.AssessmentTest.Status.Should().Be(TestStatus.Completed);
    }

    [Fact]
    public async Task Handle_KorinadiganMajburiySavolJavobsiz_Bloklanadi()
    {
        var world = new World();

        // Filtrga "1" (ko'rsatilsin) javobi beriladi — FollowUp ENDI KO'RINADI va majburiy,
        // lekin unga javob berilmagan — yakunlash bloklanishi kerak.
        world.AssessmentTest.UpsertAnswer(Guid.NewGuid(), world.FilterQuestion.Id, 1, world.FilterOptionYes.Id, 300, Now.AddMinutes(-4));

        var result = await world.Handler.Handle(new CompleteTestCommand(world.Assessment.Id, world.TestCode), CancellationToken.None);

        result.IsFailure.Should().BeTrue("FollowUp endi ko'rinadi va majburiy, lekin javobsiz");
        result.Error.Code.Should().Be("VALIDATION_ERROR");
    }

    private sealed class World
    {
        public World()
        {
            var testId = Guid.NewGuid();
            var testDefinition = TestDefinition.Create(
                testId, "SVY-COMPLETE", "Yakunlash sinovi", displayOrder: 1, estimatedMinutes: 5,
                scoringStrategyCode: null, now: Now, scoringMode: TestScoringMode.Survey, pageSize: 60);

            FilterQuestion = Question.Create(
                Guid.NewGuid(), testId, "Q_FILTER", 1, "Qo'shimcha kursga qatnashasizmi?", QuestionType.SingleChoice,
                scale: "SURVEY", scaleDirection: 1, weight: 1.0m, isRequired: true);
            FilterOptionYes = AnswerOption.Create(Guid.NewGuid(), FilterQuestion.Id, "Ha", 1, 1);
            FilterOptionNo = AnswerOption.Create(Guid.NewGuid(), FilterQuestion.Id, "Yo'q", 2, 2);
            FilterQuestion.AddOption(FilterOptionYes);
            FilterQuestion.AddOption(FilterOptionNo);
            testDefinition.AddQuestion(FilterQuestion, Now);

            var visibility = new VisibilityRule(
                VisibilityMatch.All,
                [new VisibilityCondition("Q_FILTER", VisibilityOperator.Equals, [1])]);

            FollowUpQuestion = Question.Create(
                Guid.NewGuid(), testId, "Q_FOLLOWUP", 2, "Qaysi kurs?", QuestionType.ShortText,
                scale: "SURVEY", scaleDirection: 1, weight: 1.0m, isRequired: true,
                visibilityRule: visibility);
            testDefinition.AddQuestion(FollowUpQuestion, Now);

            testDefinition.Publish(Now);

            Assessment = Assessment.Create(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "sessiya-tokeni-complete", "uz", Guid.NewGuid(),
                Now.AddMinutes(-30), Now.AddDays(7), Now.AddMinutes(-30));

            AssessmentTest = AssessmentTest.Create(Guid.NewGuid(), Assessment.Id, testId, displayOrder: 1, totalCount: 2);
            Assessment.AddTest(AssessmentTest);
            Assessment.StartTest(testId, Now.AddMinutes(-30));

            Context = new FakeBranchingSurveyAppDbContext();
            Context.TestDefinitionList.Add(testDefinition);
            Context.QuestionList.AddRange(testDefinition.Questions);
            Context.AnswerOptionList.AddRange(testDefinition.Questions.SelectMany(q => q.Options));
            Context.AssessmentList.Add(Assessment);
            Context.AssessmentTestList.Add(AssessmentTest);

            var executor = new InlineAsyncQueryExecutor();
            var cache = new PublicCatalogCache(Context, executor, new InMemoryCacheService());
            var scoringEngine = new ScoringEngine([]);

            Handler = new CompleteTestCommandHandler(Context, executor, new FakeDateTime(Now), cache, scoringEngine);
            TestCode = testDefinition.Code;
        }

        public Assessment Assessment { get; }

        public AssessmentTest AssessmentTest { get; }

        public FakeBranchingSurveyAppDbContext Context { get; }

        public CompleteTestCommandHandler Handler { get; }

        public string TestCode { get; }

        public Question FilterQuestion { get; }

        public AnswerOption FilterOptionYes { get; }

        public AnswerOption FilterOptionNo { get; }

        public Question FollowUpQuestion { get; }
    }
}
