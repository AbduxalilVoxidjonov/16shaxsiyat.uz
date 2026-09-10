using FluentAssertions;
using StudentRoadMap.Domain.Catalog.Branching;

namespace StudentRoadMap.Domain.Tests.Branching;

/// <summary>`VisibilityEvaluator` — bitta shartni baholash, har operator uchun (`docs/18` §2.5).</summary>
public sealed class VisibilityEvaluatorTests
{
    private static readonly Dictionary<string, AnswerSnapshot> Empty = new(StringComparer.Ordinal);

    [Fact]
    public void Evaluate_WithNullRule_ReturnsTrue()
    {
        VisibilityEvaluator.Evaluate(null, Empty).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_Equals_WithMatchingAnswer_ReturnsTrue()
    {
        var rule = Rule(new VisibilityCondition("Q1", VisibilityOperator.Equals, [1]));
        var answers = WithAnswer("Q1", rawValue: 1);

        VisibilityEvaluator.Evaluate(rule, answers).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_Equals_WithDifferentAnswer_ReturnsFalse()
    {
        var rule = Rule(new VisibilityCondition("Q1", VisibilityOperator.Equals, [1]));
        var answers = WithAnswer("Q1", rawValue: 2);

        VisibilityEvaluator.Evaluate(rule, answers).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_Equals_WithoutAnswer_ReturnsFalse()
    {
        var rule = Rule(new VisibilityCondition("Q1", VisibilityOperator.Equals, [1]));

        VisibilityEvaluator.Evaluate(rule, Empty).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NotEquals_WithoutAnswer_ReturnsFalse()
    {
        // docs/18 §2.5: javobsizda NotEquals HAM false — "hali javob bermagan" "boshqacha
        // qiymat" deb hisoblanmasligi uchun.
        var rule = Rule(new VisibilityCondition("Q1", VisibilityOperator.NotEquals, [1]));

        VisibilityEvaluator.Evaluate(rule, Empty).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NotEquals_WithDifferentAnswer_ReturnsTrue()
    {
        var rule = Rule(new VisibilityCondition("Q1", VisibilityOperator.NotEquals, [1]));
        var answers = WithAnswer("Q1", rawValue: 2);

        VisibilityEvaluator.Evaluate(rule, answers).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_NotEquals_WithSameAnswer_ReturnsFalse()
    {
        var rule = Rule(new VisibilityCondition("Q1", VisibilityOperator.NotEquals, [1]));
        var answers = WithAnswer("Q1", rawValue: 1);

        VisibilityEvaluator.Evaluate(rule, answers).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_AnyOf_WithValueInList_ReturnsTrue()
    {
        var rule = Rule(new VisibilityCondition("Q1", VisibilityOperator.AnyOf, [2, 3]));
        var answers = WithAnswer("Q1", rawValue: 3);

        VisibilityEvaluator.Evaluate(rule, answers).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_AnyOf_WithoutAnswer_ReturnsFalse()
    {
        var rule = Rule(new VisibilityCondition("Q1", VisibilityOperator.AnyOf, [2, 3]));

        VisibilityEvaluator.Evaluate(rule, Empty).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NoneOf_WithoutAnswer_ReturnsFalse()
    {
        var rule = Rule(new VisibilityCondition("Q1", VisibilityOperator.NoneOf, [2, 3]));

        VisibilityEvaluator.Evaluate(rule, Empty).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NoneOf_WithValueOutsideList_ReturnsTrue()
    {
        var rule = Rule(new VisibilityCondition("Q1", VisibilityOperator.NoneOf, [2, 3]));
        var answers = WithAnswer("Q1", rawValue: 5);

        VisibilityEvaluator.Evaluate(rule, answers).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_ContainsAny_WithIntersection_ReturnsTrue()
    {
        var rule = Rule(new VisibilityCondition("QM", VisibilityOperator.ContainsAny, [2, 4]));
        var answers = WithAnswer("QM", selectedValues: [1, 4]);

        VisibilityEvaluator.Evaluate(rule, answers).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_ContainsAny_WithoutIntersection_ReturnsFalse()
    {
        var rule = Rule(new VisibilityCondition("QM", VisibilityOperator.ContainsAny, [2, 4]));
        var answers = WithAnswer("QM", selectedValues: [1, 3]);

        VisibilityEvaluator.Evaluate(rule, answers).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_ContainsAll_WithAllPresent_ReturnsTrue()
    {
        var rule = Rule(new VisibilityCondition("QM", VisibilityOperator.ContainsAll, [1, 2]));
        var answers = WithAnswer("QM", selectedValues: [1, 2, 3]);

        VisibilityEvaluator.Evaluate(rule, answers).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_ContainsAll_WithSomeMissing_ReturnsFalse()
    {
        var rule = Rule(new VisibilityCondition("QM", VisibilityOperator.ContainsAll, [1, 2]));
        var answers = WithAnswer("QM", selectedValues: [1, 3]);

        VisibilityEvaluator.Evaluate(rule, answers).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_Answered_WithAnswer_ReturnsTrue()
    {
        var rule = Rule(new VisibilityCondition("QT", VisibilityOperator.Answered, []));
        var answers = WithAnswer("QT", textValue: "Toshkent");

        VisibilityEvaluator.Evaluate(rule, answers).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_Answered_WithWhitespaceText_ReturnsFalse()
    {
        var rule = Rule(new VisibilityCondition("QT", VisibilityOperator.Answered, []));
        var answers = WithAnswer("QT", textValue: "   ");

        VisibilityEvaluator.Evaluate(rule, answers).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NotAnswered_WithoutAnswer_ReturnsTrue()
    {
        var rule = Rule(new VisibilityCondition("QT", VisibilityOperator.NotAnswered, []));

        VisibilityEvaluator.Evaluate(rule, Empty).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_NotAnswered_WithAnswer_ReturnsFalse()
    {
        var rule = Rule(new VisibilityCondition("QT", VisibilityOperator.NotAnswered, []));
        var answers = WithAnswer("QT", rawValue: 1);

        VisibilityEvaluator.Evaluate(rule, answers).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_MatchAny_WithOneConditionTrue_ReturnsTrue()
    {
        var rule = new VisibilityRule(VisibilityMatch.Any,
        [
            new VisibilityCondition("Q1", VisibilityOperator.Equals, [1]),
            new VisibilityCondition("Q2", VisibilityOperator.Equals, [1]),
        ]);
        var answers = new Dictionary<string, AnswerSnapshot>(StringComparer.Ordinal)
        {
            ["Q1"] = new AnswerSnapshot(2, null, []),
            ["Q2"] = new AnswerSnapshot(1, null, []),
        };

        VisibilityEvaluator.Evaluate(rule, answers).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_MatchAll_WithOneConditionFalse_ReturnsFalse()
    {
        var rule = new VisibilityRule(VisibilityMatch.All,
        [
            new VisibilityCondition("Q1", VisibilityOperator.Equals, [1]),
            new VisibilityCondition("Q2", VisibilityOperator.Equals, [1]),
        ]);
        var answers = new Dictionary<string, AnswerSnapshot>(StringComparer.Ordinal)
        {
            ["Q1"] = new AnswerSnapshot(2, null, []),
            ["Q2"] = new AnswerSnapshot(1, null, []),
        };

        VisibilityEvaluator.Evaluate(rule, answers).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_Equals_OnMultiChoiceSourceWithSingleSelection_UsesSelectedValues()
    {
        var rule = Rule(new VisibilityCondition("QM", VisibilityOperator.Equals, [4]));
        var answers = WithAnswer("QM", selectedValues: [4]);

        VisibilityEvaluator.Evaluate(rule, answers).Should().BeTrue();
    }

    private static VisibilityRule Rule(VisibilityCondition condition) => new(VisibilityMatch.All, [condition]);

    private static Dictionary<string, AnswerSnapshot> WithAnswer(
        string code,
        int? rawValue = null,
        string? textValue = null,
        IReadOnlyList<int>? selectedValues = null) =>
        new(StringComparer.Ordinal) { [code] = new AnswerSnapshot(rawValue, textValue, selectedValues ?? []) };
}
