using FluentAssertions;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Domain.Tests.Scoring;

/// <summary>
/// `ScoringEngine` — `TestDefinition.ScoringStrategyCode` bo'yicha strategiyani topish
/// (`prompts/09-scoring-engine.md`).
/// </summary>
public sealed class ScoringEngineTests
{
    [Fact]
    public void Score_WithKnownStrategyCode_DelegatesToMatchingStrategy()
    {
        var engine = new ScoringEngine([new ActivityStrategy(), new RiasecStrategy()]);
        var input = BuildActivityInputAllThree();

        var result = engine.Score("ACTIVITY", input);

        result.IsSuccess.Should().BeTrue();
        result.Value.InterpretationKey.Should().StartWith("ACTIVITY.");
    }

    [Fact]
    public void Score_WithUnknownStrategyCode_ReturnsFailureWithClearError()
    {
        var engine = new ScoringEngine([new ActivityStrategy()]);
        var input = BuildActivityInputAllThree();

        var result = engine.Score("UNKNOWN_CODE", input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SCORING_STRATEGY_NOT_FOUND");
    }

    [Fact]
    public void Score_WithEmptyStrategyCode_ReturnsFailure()
    {
        var engine = new ScoringEngine([new ActivityStrategy()]);
        var input = BuildActivityInputAllThree();

        var result = engine.Score("", input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SCORING_STRATEGY_CODE_EMPTY");
    }

    [Fact]
    public void Constructor_WithDuplicateStrategyCodes_ThrowsDomainException()
    {
        // DI ro'yxatida bitta kod ikki marta ro'yxatdan o'tgan bo'lsa (konfiguratsiya xatosi),
        // sukut bo'yicha birini "yutqazish" o'rniga darhol aniq xato bilan to'xtatiladi.
        var act = () => new ScoringEngine([new ActivityStrategy(), new ActivityStrategy()]);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCORING_STRATEGY_DUPLICATE");
    }

    private static ScoringInput BuildActivityInputAllThree()
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();

        foreach (var scale in new[] { "MOT", "SELF", "SOCA", "ENG" })
        {
            for (var i = 0; i < 8; i++)
            {
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{scale}-{i}", scale, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = 3;
            }
        }

        return new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));
    }
}
