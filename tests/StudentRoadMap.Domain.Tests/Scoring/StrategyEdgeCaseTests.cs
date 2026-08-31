using FluentAssertions;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Domain.Tests.Scoring;

/// <summary>
/// Kontrakt buzilishi holatlari — barchasi <see cref="DomainException"/> bilan aniq to'xtaydi
/// (biznes oqim uchun emas, Application qatlami buni oldindan tekshirishi kerak bo'lgan
/// ma'lumot yaxlitligi buzilishi, `docs/12` §4).
/// </summary>
public sealed class StrategyEdgeCaseTests
{
    [Fact]
    public void Mbti16_WithMissingAnswer_ThrowsDomainException()
    {
        var questions = new List<QuestionMeta>
        {
            new(Guid.NewGuid(), "EI-1", "EI", 1, 1.0m, QuestionType.Likert5, 0),
        };
        var input = new ScoringInput(questions, new Dictionary<Guid, int>(), new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new Mbti16Strategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCORING_MISSING_ANSWER");
    }

    [Fact]
    public void Mbti16_WithMissingAxis_ThrowsDomainException()
    {
        // Faqat EI o'qi bor, SN/TF/JP yo'q.
        var (questions, answers) = BuildScale("EI", 4, 4);
        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new Mbti16Strategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCORING_SCALE_EMPTY");
    }

    [Fact]
    public void BigFive_WithWrongQuestionCountForFactor_ThrowsDomainException()
    {
        // `docs/03` §3.1: har omilda aynan 10 savol. Bu yerda faqat 4 ta beriladi.
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();
        foreach (var factor in new[] { "O", "C", "E", "A", "N" })
        {
            var count = factor == "O" ? 4 : 10;
            for (var i = 0; i < count; i++)
            {
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{factor}-{i}", factor, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = 3;
            }
        }

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new BigFiveStrategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCORING_SCALE_QUESTION_COUNT_MISMATCH");
    }

    [Fact]
    public void Riasec_WithWrongQuestionCountForType_ThrowsDomainException()
    {
        // `docs/03` §4.1: har tipda aynan 8 savol. R uchun faqat 3 ta beriladi.
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();
        foreach (var type in new[] { "R", "I", "ART", "SOC", "ENT", "CONV" })
        {
            var count = type == "R" ? 3 : 8;
            for (var i = 0; i < count; i++)
            {
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{type}-{i}", type, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = 3;
            }
        }

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new RiasecStrategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCORING_SCALE_QUESTION_COUNT_MISMATCH");
    }

    [Fact]
    public void Activity_WithWrongQuestionCountForScale_ThrowsDomainException()
    {
        // `docs/03` §5.1: har shkalada aynan 8 savol. MOT uchun faqat 5 ta beriladi.
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();
        foreach (var scale in new[] { "MOT", "SELF", "SOCA", "ENG" })
        {
            var count = scale == "MOT" ? 5 : 8;
            for (var i = 0; i < count; i++)
            {
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{scale}-{i}", scale, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = 3;
            }
        }

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new ActivityStrategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCORING_SCALE_QUESTION_COUNT_MISMATCH");
    }

    [Fact]
    public void Sum_WithoutScaleBands_ThrowsDomainException()
    {
        var (questions, answers) = BuildScale("STRESS", 4, 3);
        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new SumStrategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SUM_INTERPRETATION_BANDS_MISSING");
    }

    [Fact]
    public void Sum_WithGapInBands_ThrowsDomainExceptionWhenPercentFallsInGap()
    {
        var (questions, answers) = BuildScale("STRESS", 4, 3); // pct = 50.0

        IReadOnlyDictionary<string, IReadOnlyList<InterpretationBand>> bands = new Dictionary<string, IReadOnlyList<InterpretationBand>>
        {
            // 0-30 va 70-100 oralig'ida — 50% bu bo'shliqqa tushib qoladi.
            ["STRESS"] = [new InterpretationBand(0, 30, "Past"), new InterpretationBand(70, 100, "Yuqori")],
        };

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null), bands);

        var act = () => new SumStrategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SUM_INTERPRETATION_BAND_NOT_FOUND");
    }

    [Fact]
    public void Sum_WithNoQuestions_ThrowsDomainException()
    {
        var input = new ScoringInput([], new Dictionary<Guid, int>(), new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new SumStrategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCORING_SCALE_EMPTY");
    }

    private static (List<QuestionMeta> Questions, Dictionary<Guid, int> Answers) BuildScale(string scale, int count, int value)
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();

        for (var i = 0; i < count; i++)
        {
            var id = Guid.NewGuid();
            questions.Add(new QuestionMeta(id, $"{scale}-{i}", scale, 1, 1.0m, QuestionType.Likert5, i));
            answers[id] = value;
        }

        return (questions, answers);
    }
}
