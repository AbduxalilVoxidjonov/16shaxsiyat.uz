using FluentAssertions;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Domain.Tests.Scoring;

/// <summary>
/// `Domain/Scoring` 100% qoplamasi uchun qolgan tarmoqlar (branch)ni yopuvchi testlar —
/// boshqa fayllardagi asosiy stsenariylar tomonidan tabiiy ravishda qamralmagan holatlar.
/// </summary>
public sealed class ScoringCoverageGapTests
{
    // --- SCORING_SCALE_EMPTY: MBTI16 uchun StrategyEdgeCaseTests'da bor, qolgan 3tasi shu yerda. ---

    [Fact]
    public void BigFive_WithEntirelyMissingFactor_ThrowsScaleEmpty()
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();
        foreach (var factor in new[] { "O", "C", "E", "A" }) // "N" yo'q
        {
            for (var i = 0; i < 10; i++)
            {
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{factor}-{i}", factor, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = 3;
            }
        }

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new BigFiveStrategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCORING_SCALE_EMPTY");
    }

    [Fact]
    public void Riasec_WithEntirelyMissingType_ThrowsScaleEmpty()
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();
        foreach (var type in new[] { "R", "I", "ART", "SOC", "ENT" }) // "CONV" yo'q
        {
            for (var i = 0; i < 8; i++)
            {
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{type}-{i}", type, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = 3;
            }
        }

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new RiasecStrategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCORING_SCALE_EMPTY");
    }

    [Fact]
    public void Activity_WithEntirelyMissingScale_ThrowsScaleEmpty()
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();
        foreach (var scale in new[] { "MOT", "SELF", "SOCA" }) // "ENG" yo'q
        {
            for (var i = 0; i < 8; i++)
            {
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{scale}-{i}", scale, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = 3;
            }
        }

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new ActivityStrategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCORING_SCALE_EMPTY");
    }

    // --- RIASEC Consistency: golden testlarda faqat "High" bor, "Medium"/"Low" shu yerda. ---

    [Fact]
    public void Riasec_TopTwoTypesTwoStepsApart_ReturnsMediumConsistency()
    {
        // v=1..5 => typeRaw=8v => pct=25(v-1) (Scoring/RiasecStrategy'dagi kabi). R=5(100%),
        // ART=4(75%) — top2 = R(idx0)/ART(idx2), olti burchakdagi masofa=2 => Medium.
        var input = BuildRiasecInput(r: 5, i: 3, art: 4, soc: 2, ent: 2, conv: 1);

        var result = new RiasecStrategy().Score(input);

        result.Levels["CONSISTENCY"].Should().Be("Medium");
    }

    [Fact]
    public void Riasec_TopTwoTypesOpposite_ReturnsLowConsistency()
    {
        // R=5(100%), SOC=4(75%) — top2 = R(idx0)/SOC(idx3), olti burchakda qarama-qarshi
        // (masofa=3) => Low.
        var input = BuildRiasecInput(r: 5, i: 3, art: 2, soc: 4, ent: 2, conv: 1);

        var result = new RiasecStrategy().Score(input);

        result.Levels["CONSISTENCY"].Should().Be("Low");
    }

    // --- CompositeScorer null / to'liqsiz ACTIVITY natijasi ---

    [Fact]
    public void CompositeScorer_WithNullBigFiveResult_ReturnsFailure()
    {
        var activity = new ActivityStrategy().Score(BuildActivityInputAllThree());

        var result = CompositeScorer.ApplyMaturityIndex(null!, activity);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SCORING_COMPOSITE_INPUT_NULL");
    }

    [Fact]
    public void CompositeScorer_WithMissingActivitySelfScale_ReturnsFailure()
    {
        var bigFive = new BigFiveStrategy().Score(BuildFullBig5InputAllNeutral());
        // RIASEC natijasi "SELF" shkalasiga ega emas.
        var notActivity = new RiasecStrategy().Score(BuildFullRiasecInputAllNeutral());

        var result = CompositeScorer.ApplyMaturityIndex(bigFive, notActivity);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SCORING_COMPOSITE_MISSING_ACTIVITY");
    }

    // --- SumStrategy: Likert7 (ScoringMath.GetLikertBounds ikkinchi tarmog'i) ---

    [Fact]
    public void Sum_WithLikert7Questions_UsesSevenPointBounds()
    {
        // min=1,max=7 => scaleMin=4 (4 savol), scaleMax=28. 4 savol, hammasi v=4 (o'rtacha) =>
        // scaleRaw=16 => pct=(16-4)/(28-4)*100=12/24*100=50.0.
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();
        for (var i = 0; i < 4; i++)
        {
            var id = Guid.NewGuid();
            questions.Add(new QuestionMeta(id, $"Q{i}", "SCALE", 1, 1.0m, QuestionType.Likert7, i));
            answers[id] = 4;
        }

        IReadOnlyDictionary<string, IReadOnlyList<InterpretationBand>> bands = new Dictionary<string, IReadOnlyList<InterpretationBand>>
        {
            ["SCALE"] = [new InterpretationBand(0, 100, "O'rtacha")],
        };

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null), bands);

        var result = new SumStrategy().Score(input);

        result.RawScores["SCALE"].Should().Be(16.0);
        result.NormalizedScores["SCALE"].Should().Be(50.0);
    }

    // --- ReliabilityCalculator: bo'sh kirish va aralash yo'nalish/etishmayotgan javob ---

    [Fact]
    public void ReliabilityCalculator_WithNoQuestionsOrAnswers_ReturnsScore100()
    {
        var input = new ReliabilityInput([], new Dictionary<Guid, int>(), new Dictionary<Guid, int>(), TimeSpan.FromMinutes(10));

        var result = ReliabilityCalculator.Calculate(input);

        result.Score.Should().Be(100.0);
        result.Flag.Should().Be(ReliabilityFlag.Reliable);
        result.Reasons.Should().BeEmpty();
    }

    [Fact]
    public void ReliabilityCalculator_WithUnansweredQuestionAndSingleDirectionScale_DoesNotThrowAndSkipsReverseConflict()
    {
        // ScaleA: Q1 (dir+1, javob bor), Q2 (dir-1, javobi YO'Q — reverse-conflict hisobida
        // "continue" shoxobchasini sinaydi). ScaleB: faqat teskari yo'nalishli savollar
        // (musbat yo'q — "positives==0 && negatives>0" shoxobchasini sinaydi).
        var q1 = new QuestionMeta(Guid.NewGuid(), "A1", "ScaleA", 1, 1.0m, QuestionType.Likert5, 0);
        var q2 = new QuestionMeta(Guid.NewGuid(), "A2", "ScaleA", -1, 1.0m, QuestionType.Likert5, 1);
        var q3 = new QuestionMeta(Guid.NewGuid(), "B1", "ScaleB", -1, 1.0m, QuestionType.Likert5, 2);
        var q4 = new QuestionMeta(Guid.NewGuid(), "B2", "ScaleB", -1, 1.0m, QuestionType.Likert5, 3);

        var questions = new List<QuestionMeta> { q1, q2, q3, q4 };
        var answers = new Dictionary<Guid, int> { [q1.QuestionId] = 4, [q3.QuestionId] = 2, [q4.QuestionId] = 4 }; // q2 javobsiz
        var durations = questions.ToDictionary(q => q.QuestionId, _ => 2000);

        var input = new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(10));

        var result = ReliabilityCalculator.Calculate(input);

        result.Score.Should().Be(100.0);
        result.Flag.Should().Be(ReliabilityFlag.Reliable);
        result.Reasons.Should().BeEmpty();
    }

    // --- HollandCode / PersonalityType / ScorePercent: tenglik va ToString ---

    [Fact]
    public void HollandCode_Equality_IsValueBased()
    {
        var a = HollandCode.Create(['I', 'R', 'A']);
        var b = HollandCode.Create(['I', 'R', 'A']);
        var c = HollandCode.Create(['R', 'I', 'A']);

        a.Should().Be(b);
        a.Should().NotBe(c);
        a.ToString().Should().Be("IRA");
    }

    [Fact]
    public void PersonalityType_FromCode_WithValidCode_Succeeds()
    {
        var type = PersonalityType.FromCode("intj");

        type.Code.Should().Be("INTJ");
        type.ToString().Should().Be("INTJ");
    }

    [Fact]
    public void PersonalityType_FromCode_WithWhitespace_Throws()
    {
        var act = () => PersonalityType.FromCode("   ");

        act.Should().Throw<DomainException>().Which.Code.Should().Be("INVALID_PERSONALITY_TYPE");
    }

    [Fact]
    public void ScorePercent_Equality_IsValueBased()
    {
        var a = ScorePercent.FromClamped(42.0);
        var b = ScorePercent.FromClamped(42.0);
        var c = ScorePercent.FromClamped(43.0);

        a.Should().Be(b);
        a.Should().NotBe(c);
        a.ToString().Should().Be("42");
    }

    // --- Yordamchi qurilmalar ---

    private static ScoringInput BuildRiasecInput(int r, int i, int art, int soc, int ent, int conv)
    {
        var values = new Dictionary<string, int> { ["R"] = r, ["I"] = i, ["ART"] = art, ["SOC"] = soc, ["ENT"] = ent, ["CONV"] = conv };
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();

        foreach (var (type, value) in values)
        {
            for (var q = 0; q < 8; q++)
            {
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{type}-{q}", type, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = value;
            }
        }

        return new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));
    }

    private static ScoringInput BuildFullRiasecInputAllNeutral() => BuildRiasecInput(3, 3, 3, 3, 3, 3);

    private static ScoringInput BuildFullBig5InputAllNeutral()
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();
        foreach (var factor in new[] { "O", "C", "E", "A", "N" })
        {
            for (var i = 0; i < 10; i++)
            {
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{factor}-{i}", factor, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = 3;
            }
        }

        return new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));
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
