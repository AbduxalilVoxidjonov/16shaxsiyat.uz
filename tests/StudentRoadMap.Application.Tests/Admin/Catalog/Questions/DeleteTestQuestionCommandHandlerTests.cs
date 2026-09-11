using FluentAssertions;
using StudentRoadMap.Application.Admin.Catalog.Questions.Delete;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Application.Tests.Admin.Catalog.Testing;
using StudentRoadMap.Application.Tests.Public.Testing;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Catalog.Branching;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Tests.Admin.Catalog.Questions;

/// <summary>
/// `DeleteTestQuestionCommandHandler` — P52 (2026-09-11 QA topilmasi): egasi javobi bor savolni
/// o'chirishga urindi, `fk_answers_questions_question_id` (`23503`) jimgina `500
/// INTERNAL_ERROR` bo'lib chiqdi. Endi o'chirishdan OLDIN `EXISTS` (`AnyAsync`, `COUNT` EMAS)
/// bilan tekshiriladi va aniq `409 QUESTION_IN_USE` qaytariladi. DB/HTTP'siz sinov — soxta
/// `IAppDbContext` (`FakeCatalogQuestionsAppDbContext`).
/// </summary>
public sealed class DeleteTestQuestionCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static TestDefinition CreateSurveyTestDefinition(string code = "SURVEY-1") => TestDefinition.Create(
        Guid.NewGuid(),
        code,
        "So'rovnoma",
        1,
        10,
        scoringStrategyCode: null,
        Now,
        kind: TestKind.Custom,
        isSystem: false,
        scoringMode: TestScoringMode.Survey);

    private static Question CreateQuestion(Guid testDefinitionId, string code = "Q1") =>
        Question.Create(Guid.NewGuid(), testDefinitionId, code, 1, "Savol matni", QuestionType.Likert5, "SURVEY", 1, 1.0m);

    private static (DeleteTestQuestionCommandHandler Handler, FakeCatalogQuestionsAppDbContext Context) CreateHandler()
    {
        var context = new FakeCatalogQuestionsAppDbContext();
        var executor = new CatalogQuestionsInlineAsyncQueryExecutor();
        var cache = new PublicCatalogCache(context, executor, new InMemoryCacheService());
        var handler = new DeleteTestQuestionCommandHandler(context, executor, new FakeCatalogDateTime(Now), new FakeCatalogIpHasher(), cache);

        return (handler, context);
    }

    [Fact]
    public async Task Handle_QuestionWithAnswers_ReturnsQuestionInUse()
    {
        var (handler, context) = CreateHandler();
        var test = CreateSurveyTestDefinition();
        var question = CreateQuestion(test.Id);
        test.AddQuestion(question, Now);
        context.TestDefinitionList.Add(test);
        context.QuestionList.Add(question);
        context.AnswerList.Add(Answer.Create(Guid.NewGuid(), Guid.NewGuid(), question.Id, 3, null, 1200, Now));

        var result = await handler.Handle(new DeleteTestQuestionCommand(question.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("QUESTION_IN_USE");
        result.Error.Message.Should().Contain("Faol emas", "xabar harakatga yo'naltiruvchi bo'lishi kerak — shunchaki 'bo'lmaydi' emas");
        test.QuestionCount.Should().Be(1, "javobi bor savol HALI o'chirilmagan bo'lishi kerak");
        context.SaveChangesCalled.Should().BeFalse("EXISTS tekshiruvi saqlashdan OLDIN to'xtatishi kerak");
    }

    [Fact]
    public async Task Handle_QuestionWithoutAnswers_RemovesQuestion()
    {
        // Regressiya: avvalgidek ishlashi kerak — javobsiz savol muvaffaqiyatli o'chadi.
        var (handler, context) = CreateHandler();
        var test = CreateSurveyTestDefinition();
        var question = CreateQuestion(test.Id);
        test.AddQuestion(question, Now);
        context.TestDefinitionList.Add(test);
        context.QuestionList.Add(question);

        var result = await handler.Handle(new DeleteTestQuestionCommand(question.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        test.QuestionCount.Should().Be(0);
        context.SaveChangesCalled.Should().BeTrue();
        context.AuditLogList.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_QuestionReferencedByVisibility_PropagatesDomainException()
    {
        // Domen darajasidagi xabar tafsiloti (`QuestionSectionTests`da sinalgan) — bu yerda
        // FAQAT handler to'g'ri simlanganini (o'tkazib yubormasligini) tasdiqlaydi.
        var (handler, context) = CreateHandler();
        var test = CreateSurveyTestDefinition();
        var gate = CreateQuestion(test.Id, "Q1");
        test.AddQuestion(gate, Now);
        var rule = new VisibilityRule(VisibilityMatch.All, [new VisibilityCondition("Q1", VisibilityOperator.Answered, [])]);
        var dependent = Question.Create(Guid.NewGuid(), test.Id, "Q2", 2, "Bog'liq savol", QuestionType.ShortText, "SURVEY", 1, 1.0m, visibilityRule: rule);
        test.AddQuestion(dependent, Now);
        context.TestDefinitionList.Add(test);
        context.QuestionList.Add(gate);
        context.QuestionList.Add(dependent);

        var act = async () => await handler.Handle(new DeleteTestQuestionCommand(gate.Id, Guid.NewGuid()), CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<DomainException>()).Which;
        ex.Code.Should().Be("QUESTION_REFERENCED_BY_VISIBILITY");
        context.SaveChangesCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_QuestionNotFound_ReturnsNotFound()
    {
        var (handler, _) = CreateHandler();

        var result = await handler.Handle(new DeleteTestQuestionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOT_FOUND");
    }
}
