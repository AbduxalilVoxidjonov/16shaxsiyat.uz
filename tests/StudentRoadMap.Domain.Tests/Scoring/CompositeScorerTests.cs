using FluentAssertions;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Domain.Tests.Scoring;

/// <summary>
/// `CompositeScorer.ApplyMaturityIndex` — `docs/03-psixologik-metodikalar.md` §3.3:
/// `MaturityIndex = 0.30*C + 0.25*Stability + 0.20*A + 0.15*SELF + 0.10*O`.
/// </summary>
public sealed class CompositeScorerTests
{
    [Fact]
    public void ApplyMaturityIndex_WithHandComputedInputs_ReturnsExpectedIndexAndLevel()
    {
        // Qo'lda hisob (docs/03 §3.2 formulasi bilan, 5 ijobiy + 5 teskari savol har omilda):
        // C: pos=4,neg=2 => factorRaw=5*4+5*(6-2)=40 => C_pct=(40-10)/40*100=75.0
        // N: pos=2,neg=4 => factorRaw=5*2+5*(6-4)=20 => N_pct=(20-10)/40*100=25.0 => Stability=100-25=75.0
        // A: pos=3,neg=2 => factorRaw=5*3+5*(6-2)=35 => A_pct=(35-10)/40*100=62.5
        // O: pos=neg=3   => factorRaw=30              => O_pct=(30-10)/40*100=50.0
        // SELF (ACTIVITY, 4 ijobiy+4 teskari): pos=5,neg=2 => scaleRaw=4*5+4*(6-2)=36 => SELF_pct=(36-8)/32*100=87.5
        // MaturityIndex = 0.30*75 + 0.25*75 + 0.20*62.5 + 0.15*87.5 + 0.10*50
        //               = 22.5 + 18.75 + 12.5 + 13.125 + 5.0 = 71.875 => AwayFromZero(2dp) = 71.88
        // 71.88 <= 75 (MaturityLevelGoodMax) => "Yaxshi".
        var bigFive = new BigFiveStrategy().Score(BuildBig5Input(cPos: 4, cNeg: 2, nPos: 2, nNeg: 4, aPos: 3, aNeg: 2, oPos: 3, oNeg: 3));
        var activity = new ActivityStrategy().Score(BuildActivityInput(selfRaw: 36));

        var result = CompositeScorer.ApplyMaturityIndex(bigFive, activity);

        result.IsSuccess.Should().BeTrue();
        result.Value.CompositeIndex.Should().NotBeNull();
        result.Value.CompositeIndex!.Value.Should().BeApproximately(71.88, 0.01);
        result.Value.Levels["MATURITY"].Should().Be("Yaxshi");

        // BIG5'ning o'z shkala natijalari o'zgarmasdan saqlanishi kerak (faqat kengaytiriladi).
        result.Value.NormalizedScores["C"].Should().BeApproximately(75.0, 0.01);
        result.Value.Levels["C"].Should().Be("Yuqori");
    }

    [Theory]
    // Har beshala kirish (C, Stability, A, SELF, O) bir xil Y qiymatga tenglashtirilgan — og'irliklar
    // yig'indisi 1.0 bo'lgani uchun MaturityIndex = Y bo'lishi kerak (docs/03 §3.3 daraja jadvali).
    [InlineData(25.0, "Shakllanish bosqichida")]   // <=35
    [InlineData(37.5, "O'rtacha")]                  // 36..55
    [InlineData(62.5, "Yaxshi")]                     // 56..75
    [InlineData(87.5, "Yuqori")]                     // 76..100
    public void ApplyMaturityIndex_ClassifiesLevel_ByDocumentedBoundaries(double uniformY, string expectedLevel)
    {
        var cAndAFactorRaw = 10 + (int)Math.Round(uniformY * 0.4, MidpointRounding.AwayFromZero);
        var nFactorRaw = 10 + (int)Math.Round((100 - uniformY) * 0.4, MidpointRounding.AwayFromZero);
        var selfScaleRaw = 8 + (int)Math.Round(uniformY * 0.32, MidpointRounding.AwayFromZero);

        var bigFive = new BigFiveStrategy().Score(BuildUniformBig5Input(cFactorRaw: cAndAFactorRaw, nFactorRaw: nFactorRaw, aFactorRaw: cAndAFactorRaw, oFactorRaw: cAndAFactorRaw));
        var activity = new ActivityStrategy().Score(BuildActivityInput(selfRaw: selfScaleRaw));

        var result = CompositeScorer.ApplyMaturityIndex(bigFive, activity);

        result.IsSuccess.Should().BeTrue();
        result.Value.CompositeIndex!.Value.Should().BeApproximately(uniformY, 0.01);
        result.Value.Levels["MATURITY"].Should().Be(expectedLevel);
    }

    [Fact]
    public void ApplyMaturityIndex_WithNonBig5Result_ReturnsFailure()
    {
        var notBig5 = new RiasecStrategy().Score(BuildRiasecInputForFailureTest());
        var activity = new ActivityStrategy().Score(BuildActivityInput(selfRaw: 24));

        var result = CompositeScorer.ApplyMaturityIndex(notBig5, activity);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SCORING_COMPOSITE_MISSING_BIG5");
    }

    // --- Yordamchi qurilmalar ---

    private static ScoringInput BuildBig5Input(int cPos, int cNeg, int nPos, int nNeg, int aPos, int aNeg, int oPos, int oNeg)
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();

        AddFactor(questions, answers, "C", cPos, cNeg);
        AddFactor(questions, answers, "N", nPos, nNeg);
        AddFactor(questions, answers, "A", aPos, aNeg);
        AddFactor(questions, answers, "O", oPos, oNeg);
        AddFactor(questions, answers, "E", 3, 3); // Maturity hisobida ishlatilmaydi, ixtiyoriy qiymat.

        return new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        static void AddFactor(List<QuestionMeta> qs, Dictionary<Guid, int> ans, string factor, int posValue, int negValue)
        {
            for (var i = 0; i < 5; i++)
            {
                var posId = Guid.NewGuid();
                qs.Add(new QuestionMeta(posId, $"{factor}-P{i}", factor, 1, 1.0m, QuestionType.Likert5, qs.Count));
                ans[posId] = posValue;

                var negId = Guid.NewGuid();
                qs.Add(new QuestionMeta(negId, $"{factor}-N{i}", factor, -1, 1.0m, QuestionType.Likert5, qs.Count));
                ans[negId] = negValue;
            }
        }
    }

    private static ScoringInput BuildUniformBig5Input(int cFactorRaw, int nFactorRaw, int aFactorRaw, int oFactorRaw)
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();

        AddFactorBySum(questions, answers, "C", cFactorRaw);
        AddFactorBySum(questions, answers, "N", nFactorRaw);
        AddFactorBySum(questions, answers, "A", aFactorRaw);
        AddFactorBySum(questions, answers, "O", oFactorRaw);
        AddFactorBySum(questions, answers, "E", 30); // Ixtiyoriy, maturity hisobida ishlatilmaydi.

        return new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        static void AddFactorBySum(List<QuestionMeta> qs, Dictionary<Guid, int> ans, string factor, int targetSum)
        {
            foreach (var value in BuildAnswersSummingTo(targetSum, 10))
            {
                var id = Guid.NewGuid();
                qs.Add(new QuestionMeta(id, $"{factor}-{qs.Count}", factor, 1, 1.0m, QuestionType.Likert5, qs.Count));
                ans[id] = value;
            }
        }
    }

    private static ScoringInput BuildActivityInput(int selfRaw)
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();

        AddScale(questions, answers, "MOT", 24);
        AddScale(questions, answers, "SELF", selfRaw);
        AddScale(questions, answers, "SOCA", 24);
        AddScale(questions, answers, "ENG", 24);

        return new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        static void AddScale(List<QuestionMeta> qs, Dictionary<Guid, int> ans, string scale, int targetSum)
        {
            foreach (var value in BuildAnswersSummingTo(targetSum, 8))
            {
                var id = Guid.NewGuid();
                qs.Add(new QuestionMeta(id, $"{scale}-{qs.Count}", scale, 1, 1.0m, QuestionType.Likert5, qs.Count));
                ans[id] = value;
            }
        }
    }

    private static ScoringInput BuildRiasecInputForFailureTest()
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

    /// <summary>`count` ta [1,5] oralig'idagi butun sonni yig'indisi `target` ga teng qilib taqsimlaydi.</summary>
    private static List<int> BuildAnswersSummingTo(int target, int count)
    {
        var baseValue = target / count;
        var remainder = target % count;

        var values = new List<int>(count);
        for (var i = 0; i < count; i++)
        {
            values.Add(baseValue + (i < remainder ? 1 : 0));
        }

        return values;
    }
}
