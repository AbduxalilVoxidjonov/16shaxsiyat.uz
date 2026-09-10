using FluentAssertions;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Application.Public.GetTestQuestions;
using StudentRoadMap.Application.Tests.Admin.Ai.Testing;
using StudentRoadMap.Application.Tests.Public.Testing;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Tests.Public;

/// <summary>
/// `GetTestQuestionsQueryHandler` — `docs/18` §4.1: anketada bo'lim bo'lsa sahifalash O'CHADI
/// (`page = 1`, `totalPages = 1`, BARCHA faol savollar), bo'limsiz anketada mavjud `pageSize`
/// sahifalash AYNAN o'zgarishsiz qoladi (regressiya).
/// </summary>
public sealed class GetTestQuestionsSectionedTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_BolimliAnketa_SahifalashOchadiVaHammaSavolQaytadi()
    {
        var world = new World(sectioned: true, pageSize: 2, questionCount: 3);

        var result = await world.HandleAsync(page: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.TotalPages.Should().Be(1, "bo'lim bo'lsa sahifalash o'chadi (docs/18 §4.1)");
        result.Value.TotalQuestions.Should().Be(3);
        result.Value.Questions.Should().HaveCount(3, "pageSize=2 bo'lsa ham HAMMA faol savol qaytishi kerak");
        result.Value.Sections.Should().NotBeNull();
        result.Value.Sections!.Should().ContainSingle();
        result.Value.Sections![0].Code.Should().Be("S1");
    }

    [Fact]
    public async Task Handle_BolimsizAnketa_MavjudSahifalashOzgarishsizQoladi()
    {
        var world = new World(sectioned: false, pageSize: 2, questionCount: 3);

        var result = await world.HandleAsync(page: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.TotalPages.Should().Be(2, "ceil(3/2) — mavjud sahifalash o'zgarishsiz (regressiya)");
        result.Value.Questions.Should().HaveCount(2);
        result.Value.Sections.Should().BeNull("bo'limsiz anketada `sections` har doim `null`");
    }

    private sealed class World
    {
        public World(bool sectioned, int pageSize, int questionCount)
        {
            var testId = Guid.NewGuid();
            var testDefinition = TestDefinition.Create(
                testId,
                "SVY-SECT",
                "Bo'limli sinov anketasi",
                displayOrder: 1,
                estimatedMinutes: 5,
                scoringStrategyCode: null,
                now: Now,
                pageSize: pageSize,
                scoringMode: TestScoringMode.Survey);

            if (sectioned)
            {
                var section = QuestionSection.Create(Guid.NewGuid(), testId, "S1", "Bo'lim 1", displayOrder: 1);
                testDefinition.AddSection(section, Now);

                for (var i = 1; i <= questionCount; i++)
                {
                    var question = Question.Create(
                        Guid.NewGuid(), testId, $"Q{i}", i, $"Savol {i}", QuestionType.ShortText,
                        scale: "SURVEY", scaleDirection: 1, weight: 1.0m, isRequired: true,
                        sectionId: section.Id);
                    testDefinition.AddQuestion(question, Now);
                }
            }
            else
            {
                for (var i = 1; i <= questionCount; i++)
                {
                    var question = Question.Create(
                        Guid.NewGuid(), testId, $"Q{i}", i, $"Savol {i}", QuestionType.ShortText,
                        scale: "SURVEY", scaleDirection: 1, weight: 1.0m, isRequired: true);
                    testDefinition.AddQuestion(question, Now);
                }
            }

            testDefinition.Publish(Now);

            var programId = Guid.NewGuid();
            Assessment = Assessment.Create(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "sessiya-tokeni-sect", "uz", programId,
                Now.AddMinutes(-10), Now.AddDays(7), Now.AddMinutes(-10));

            var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), Assessment.Id, testId, displayOrder: 1, totalCount: questionCount);
            Assessment.AddTest(assessmentTest);
            Assessment.StartTest(testId, Now.AddMinutes(-10));

            Context = new FakeBranchingSurveyAppDbContext();
            Context.TestDefinitionList.Add(testDefinition);
            Context.QuestionList.AddRange(testDefinition.Questions);
            Context.QuestionSectionList.AddRange(testDefinition.Sections);
            Context.AssessmentList.Add(Assessment);
            Context.AssessmentTestList.Add(assessmentTest);

            var executor = new InlineAsyncQueryExecutor();
            var cache = new PublicCatalogCache(Context, executor, new InMemoryCacheService());

            Handler = new GetTestQuestionsQueryHandler(Context, executor, new FakeDateTime(Now), cache);
            TestCode = testDefinition.Code;
        }

        public Assessment Assessment { get; }

        public FakeBranchingSurveyAppDbContext Context { get; }

        public GetTestQuestionsQueryHandler Handler { get; }

        public string TestCode { get; }

        public Task<Domain.Common.Result<GetTestQuestionsResult>> HandleAsync(int page) =>
            Handler.Handle(new GetTestQuestionsQuery(Assessment.Id, TestCode, page), CancellationToken.None);
    }
}
