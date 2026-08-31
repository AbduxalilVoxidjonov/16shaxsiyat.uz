using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// Aktivlik va motivatsiya (`ACTIVITY`) — `docs/03-psixologik-metodikalar.md` §5.
/// </summary>
public sealed class ActivityStrategy : IScoringStrategy
{
    private static readonly string[] ScaleOrder = ["MOT", "SELF", "SOCA", "ENG"];

    public string StrategyCode => "ACTIVITY";

    public ScoringResult Score(ScoringInput input)
    {
        var byScale = ScoringMath.GroupByScaleOrdered(input.Questions);

        var rawScores = new Dictionary<string, double>(StringComparer.Ordinal);
        var normalizedScores = new Dictionary<string, double>(StringComparer.Ordinal);
        var pctByScale = new Dictionary<string, double>(StringComparer.Ordinal);

        foreach (var scale in ScaleOrder)
        {
            if (!byScale.TryGetValue(scale, out var scaleQuestions) || scaleQuestions.Count == 0)
            {
                throw new DomainException("SCORING_SCALE_EMPTY", $"'{scale}' shkalasi uchun savol topilmadi.");
            }

            // `scalePct` formulasi (8..40 xom ball) aynan 8 savolga mo'ljallangan konstanta
            // (`docs/03` §5.1: "32 savol, 4 shkala × 8 savol") — boshqa son bilan `(raw-8)/32*100`
            // natijasi 0..100 tashqarisiga chiqadi, shuning uchun soni qat'iy tekshiriladi.
            if (scaleQuestions.Count != ScoringConstants.ActivityQuestionsPerScale)
            {
                throw new DomainException(
                    "SCORING_SCALE_QUESTION_COUNT_MISMATCH",
                    $"'{scale}' shkalasida {ScoringConstants.ActivityQuestionsPerScale} ta savol bo'lishi kerak, topildi: {scaleQuestions.Count}.");
            }

            double scaleRaw = 0;
            foreach (var question in scaleQuestions)
            {
                ScoringMath.EnsureUnitWeight(question);
                var value = ScoringMath.GetRequiredAnswer(input.Answers, question, ScoringConstants.Likert5Min, ScoringConstants.Likert5Max);
                scaleRaw += ScoringMath.ApplyDirection(value, question.Direction, ScoringConstants.Likert5Min, ScoringConstants.Likert5Max);
            }

            var scalePctRaw = (scaleRaw - ScoringConstants.ActivityScaleRawMin) / ScoringConstants.ActivityScaleRawRange * 100.0;
            var scalePct = ScorePercent.FromClamped(scalePctRaw).Value;

            rawScores[scale] = scaleRaw;
            normalizedScores[scale] = scalePct;
            pctByScale[scale] = scalePct;
        }

        var activityIndexRaw =
            ScoringConstants.ActivityWeightMotivation * pctByScale["MOT"] +
            ScoringConstants.ActivityWeightSelfRegulation * pctByScale["SELF"] +
            ScoringConstants.ActivityWeightSocialActivity * pctByScale["SOCA"] +
            ScoringConstants.ActivityWeightEngagement * pctByScale["ENG"];

        // QA Bloklovchi-1 audit talabi: daraja tasnifi va `NeedsAttention` chegarasi ham
        // yaxlitlangan qiymat bo'yicha tekshiriladi (30/50/70/85/31 chegaralarida xuddi shu
        // IEEE-754 xatosi bo'lishi mumkin edi).
        var activityIndex = ScorePercent.FromClamped(activityIndexRaw).Value;
        var levelCode = ClassifyActivityLevel(activityIndex);
        var needsAttention = activityIndex < ScoringConstants.ActivityNeedsAttentionThreshold;

        var levels = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ACTIVITY"] = levelCode,
        };

        var flags = new List<string>();
        if (needsAttention)
        {
            flags.Add("NeedsAttention");
        }

        return new ScoringResult(
            ResultCode: null,
            rawScores,
            normalizedScores,
            levels,
            CompositeIndex: activityIndex,
            Flags: flags,
            InterpretationKey: $"ACTIVITY.{levelCode}",
            ScoringVersion: ScoringConstants.CurrentScoringVersion);
    }

    private static string ClassifyActivityLevel(double activityIndex) => activityIndex switch
    {
        <= ScoringConstants.ActivityLevelPassiveMax => ScoringConstants.ActivityLevelPassiveCode,
        <= ScoringConstants.ActivityLevelLowActiveMax => ScoringConstants.ActivityLevelLowActiveCode,
        <= ScoringConstants.ActivityLevelModerateMax => ScoringConstants.ActivityLevelModerateCode,
        <= ScoringConstants.ActivityLevelActiveMax => ScoringConstants.ActivityLevelActiveCode,
        _ => ScoringConstants.ActivityLevelHighlyActiveCode,
    };
}
