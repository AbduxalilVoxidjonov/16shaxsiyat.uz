using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// `BIG5` va `ACTIVITY` natijalari tayyor bo'lgach `MaturityIndex` ni hisoblaydi
/// (`docs/03-psixologik-metodikalar.md` §3.3). Alohida qatlam — strategiyalar bir-birini
/// chaqirmaydi degan qoidaga rioya qilib, ikkala tayyor natijani tashqaridan qabul qiladi.
/// </summary>
public static class CompositeScorer
{
    /// <summary>
    /// `bigFiveResult` ning nusxasini `CompositeIndex = MaturityIndex` va
    /// `Levels["MATURITY"] = daraja` bilan qaytaradi. Kirish natijalarida kerakli shkalalar
    /// yo'q bo'lsa (noto'g'ri strategiya natijasi uzatilgan bo'lsa) `Result.Failure` qaytaradi.
    /// </summary>
    public static Result<ScoringResult> ApplyMaturityIndex(ScoringResult bigFiveResult, ScoringResult activityResult)
    {
        if (bigFiveResult is null || activityResult is null)
        {
            return Result.Failure<ScoringResult>(new Error("SCORING_COMPOSITE_INPUT_NULL", "BIG5 va ACTIVITY natijalari bo'sh bo'lishi mumkin emas."));
        }

        if (!TryGet(bigFiveResult.NormalizedScores, "C", out var cPct) ||
            !TryGet(bigFiveResult.NormalizedScores, "STABILITY", out var stabilityPct) ||
            !TryGet(bigFiveResult.NormalizedScores, "A", out var aPct) ||
            !TryGet(bigFiveResult.NormalizedScores, "O", out var oPct))
        {
            return Result.Failure<ScoringResult>(new Error("SCORING_COMPOSITE_MISSING_BIG5", "BIG5 natijasida C/STABILITY/A/O shkalalari topilmadi."));
        }

        if (!TryGet(activityResult.NormalizedScores, "SELF", out var selfPct))
        {
            return Result.Failure<ScoringResult>(new Error("SCORING_COMPOSITE_MISSING_ACTIVITY", "ACTIVITY natijasida SELF shkalasi topilmadi."));
        }

        var maturityIndexRaw =
            ScoringConstants.MaturityWeightConscientiousness * cPct +
            ScoringConstants.MaturityWeightStability * stabilityPct +
            ScoringConstants.MaturityWeightAgreeableness * aPct +
            ScoringConstants.MaturityWeightSelfRegulation * selfPct +
            ScoringConstants.MaturityWeightOpenness * oPct;

        // QA Bloklovchi-1 audit talabi: daraja tasnifi (35/55/75 chegaralari) yaxlitlangan
        // qiymat bo'yicha, saqlanadigan `CompositeIndex` bilan bir xil sondan qilinadi.
        var maturityIndex = ScorePercent.FromClamped(maturityIndexRaw).Value;
        var maturityLevel = ClassifyMaturityLevel(maturityIndex);

        var levels = new Dictionary<string, string>(bigFiveResult.Levels, StringComparer.Ordinal)
        {
            ["MATURITY"] = maturityLevel,
        };

        var updated = bigFiveResult with
        {
            CompositeIndex = maturityIndex,
            Levels = levels,
        };

        return Result.Success(updated);
    }

    private static bool TryGet(IReadOnlyDictionary<string, double> scores, string key, out double value) =>
        scores.TryGetValue(key, out value);

    private static string ClassifyMaturityLevel(double maturityIndex) => maturityIndex switch
    {
        <= ScoringConstants.MaturityLevelFormingMax => ScoringConstants.MaturityLevelForming,
        <= ScoringConstants.MaturityLevelAverageMax => ScoringConstants.MaturityLevelAverage,
        <= ScoringConstants.MaturityLevelGoodMax => ScoringConstants.MaturityLevelGood,
        _ => ScoringConstants.MaturityLevelHigh,
    };
}
