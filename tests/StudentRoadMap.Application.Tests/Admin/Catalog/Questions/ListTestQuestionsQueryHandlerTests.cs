using FluentAssertions;
using StudentRoadMap.Application.Admin.Catalog.Questions.List;
using StudentRoadMap.Application.Tests.Admin.Catalog.Testing;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Tests.Admin.Catalog.Questions;

/// <summary>
/// `ListTestQuestionsQueryHandler` — `HasAnswers` maydoni (P52, 2026-09-11 QA topilmasi):
/// frontend o'chirish tugmasini OLDINDAN o'chirib qo'yishi uchun. Bitta BATCH so'rov bilan
/// hisoblanishi (N+1 EMAS, ADR-11) alohida sinaladi.
/// </summary>
public sealed class ListTestQuestionsQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static TestDefinition CreateCustomTestDefinition() => TestDefinition.Create(
        Guid.NewGuid(),
        "CUSTOM-1",
        "Maxsus anketa",
        1,
        10,
        "SUM",
        Now,
        kind: TestKind.Custom,
        isSystem: false);

    private static Question CreateQuestion(Guid testDefinitionId, string code) =>
        Question.Create(Guid.NewGuid(), testDefinitionId, code, 1, "Savol matni", QuestionType.Likert5, "SCALE", 1, 1.0m);

    [Fact]
    public async Task Handle_SavollarBirXilJavobBorYoqBoyichaTogriHasAnswersQaytaradi()
    {
        var context = new FakeCatalogQuestionsAppDbContext();
        var executor = new CatalogQuestionsInlineAsyncQueryExecutor();
        var test = CreateCustomTestDefinition();
        var answered = CreateQuestion(test.Id, "Q1");
        var unanswered = CreateQuestion(test.Id, "Q2");
        context.TestDefinitionList.Add(test);
        context.QuestionList.AddRange([answered, unanswered]);
        context.AnswerList.Add(Answer.Create(Guid.NewGuid(), Guid.NewGuid(), answered.Id, 3, null, 1000, Now));

        var handler = new ListTestQuestionsQueryHandler(context, executor);

        var result = await handler.Handle(new ListTestQuestionsQuery(test.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Single(q => q.Code == "Q1").HasAnswers.Should().BeTrue();
        result.Value.Single(q => q.Code == "Q2").HasAnswers.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_HasAnswers_BittaBatchSorovBilanHisoblanadi()
    {
        // N+1 bo'lganda savollar soni oshgani sari `ToListAsync` chaqiruvlari ham oshar edi —
        // bu yerda 6 ta savol bilan ham chaqiruvlar soni O'ZGARMAS (ADR-11).
        var context = new FakeCatalogQuestionsAppDbContext();
        var countingExecutor = new CountingCatalogQuestionsInlineAsyncQueryExecutor();
        var test = CreateCustomTestDefinition();
        context.TestDefinitionList.Add(test);

        for (var i = 1; i <= 6; i++)
        {
            var question = CreateQuestion(test.Id, $"Q{i}");
            context.QuestionList.Add(question);

            if (i % 2 == 0)
            {
                context.AnswerList.Add(Answer.Create(Guid.NewGuid(), Guid.NewGuid(), question.Id, 1, null, 500, Now));
            }
        }

        var handler = new ListTestQuestionsQueryHandler(context, countingExecutor);

        var result = await handler.Handle(new ListTestQuestionsQuery(test.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Count(q => q.HasAnswers).Should().Be(3);

        // `LoadScaleNamesAsync` (TestScales) + `Questions` + `AnswerOptions` + `LoadQuestionIdsWithAnswersAsync`
        // (Answers) — savollar soniga bog'liq BO'LMAGAN, DOIM to'rtta `ToListAsync` chaqiruvi.
        countingExecutor.ToListAsyncCallCount.Should().Be(4);
    }

    [Fact]
    public async Task Handle_JavobYoqBolsa_HammaSavolHasAnswersFalse()
    {
        var context = new FakeCatalogQuestionsAppDbContext();
        var executor = new CatalogQuestionsInlineAsyncQueryExecutor();
        var test = CreateCustomTestDefinition();
        var question = CreateQuestion(test.Id, "Q1");
        context.TestDefinitionList.Add(test);
        context.QuestionList.Add(question);

        var handler = new ListTestQuestionsQueryHandler(context, executor);

        var result = await handler.Handle(new ListTestQuestionsQuery(test.Id), CancellationToken.None);

        result.Value.Should().ContainSingle().Which.HasAnswers.Should().BeFalse();
    }
}
