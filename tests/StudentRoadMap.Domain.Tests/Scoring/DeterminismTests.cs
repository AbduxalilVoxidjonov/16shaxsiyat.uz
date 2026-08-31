using FluentAssertions;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Domain.Tests.Scoring;

/// <summary>
/// Determinizm — `prompts/09-scoring-engine.md`: "bir xil kirish → doim bir xil chiqish",
/// `Random`/`DateTime`/`Guid.NewGuid()`ga bog'liqlik yo'q, kolleksiya tartibiga bog'liq emas.
/// </summary>
public sealed class DeterminismTests
{
    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void Score_CalledOneHundredTimes_AlwaysReturnsIdenticalResult(IScoringStrategy strategy, ScoringInput input)
    {
        var first = strategy.Score(input);

        for (var i = 0; i < 100; i++)
        {
            var repeated = strategy.Score(input);

            repeated.ResultCode.Should().Be(first.ResultCode);
            repeated.RawScores.Should().BeEquivalentTo(first.RawScores);
            repeated.NormalizedScores.Should().BeEquivalentTo(first.NormalizedScores);
            repeated.Levels.Should().BeEquivalentTo(first.Levels);
            repeated.CompositeIndex.Should().Be(first.CompositeIndex);
            repeated.Flags.Should().Equal(first.Flags);
            repeated.InterpretationKey.Should().Be(first.InterpretationKey);
        }
    }

    [Theory]
    [MemberData(nameof(AllStrategies))]
    public void Score_WithShuffledQuestionOrder_ReturnsSameResult(IScoringStrategy strategy, ScoringInput input)
    {
        var baseline = strategy.Score(input);

        // Savollar ro'yxatini teskari tartibga o'tkazamiz (mazmuni bir xil, faqat tartib boshqa).
        var shuffledInput = input with { Questions = input.Questions.Reverse().ToList() };

        var shuffled = strategy.Score(shuffledInput);

        shuffled.ResultCode.Should().Be(baseline.ResultCode);
        shuffled.RawScores.Should().BeEquivalentTo(baseline.RawScores);
        shuffled.NormalizedScores.Should().BeEquivalentTo(baseline.NormalizedScores);
        shuffled.CompositeIndex.Should().Be(baseline.CompositeIndex);
    }

    public static IEnumerable<object[]> AllStrategies()
    {
        yield return [new Mbti16Strategy(), BuildMbtiInput()];
        yield return [new BigFiveStrategy(), BuildBigFiveInput()];
        yield return [new RiasecStrategy(), BuildRiasecInput()];
        yield return [new ActivityStrategy(), BuildActivityInput()];
        yield return [new SumStrategy(), BuildSumInput()];
    }

    private static ScoringInput BuildMbtiInput()
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();

        foreach (var axis in new[] { "EI", "SN", "TF", "JP" })
        {
            AddPair(questions, answers, axis, 1, 4);
            AddPair(questions, answers, axis, -1, 2);
        }

        return new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        static void AddPair(List<QuestionMeta> qs, Dictionary<Guid, int> ans, string scale, int direction, int value)
        {
            for (var i = 0; i < 2; i++)
            {
                var id = Guid.NewGuid();
                qs.Add(new QuestionMeta(id, $"{scale}-{direction}-{i}", scale, direction, 1.0m, QuestionType.Likert5, qs.Count));
                ans[id] = value;
            }
        }
    }

    private static ScoringInput BuildBigFiveInput()
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();

        foreach (var factor in new[] { "O", "C", "E", "A", "N" })
        {
            for (var i = 0; i < 5; i++)
            {
                var posId = Guid.NewGuid();
                questions.Add(new QuestionMeta(posId, $"{factor}-P{i}", factor, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[posId] = 4;

                var negId = Guid.NewGuid();
                questions.Add(new QuestionMeta(negId, $"{factor}-N{i}", factor, -1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[negId] = 2;
            }
        }

        return new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));
    }

    private static ScoringInput BuildRiasecInput()
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();

        foreach (var type in new[] { "R", "I", "ART", "SOC", "ENT", "CONV" })
        {
            for (var i = 0; i < 8; i++)
            {
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{type}-{i}", type, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = 3;
            }
        }

        return new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));
    }

    private static ScoringInput BuildActivityInput()
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

    private static ScoringInput BuildSumInput()
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();

        foreach (var scale in new[] { "STRESS", "SUPPORT" })
        {
            for (var i = 0; i < 4; i++)
            {
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{scale}-{i}", scale, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = 3;
            }
        }

        IReadOnlyDictionary<string, IReadOnlyList<InterpretationBand>> bands = new Dictionary<string, IReadOnlyList<InterpretationBand>>
        {
            ["STRESS"] = [new InterpretationBand(0, 33, "Past"), new InterpretationBand(34, 66, "O'rtacha"), new InterpretationBand(67, 100, "Yuqori")],
            ["SUPPORT"] = [new InterpretationBand(0, 33, "Past"), new InterpretationBand(34, 66, "O'rtacha"), new InterpretationBand(67, 100, "Yuqori")],
        };

        return new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null), bands);
    }
}
