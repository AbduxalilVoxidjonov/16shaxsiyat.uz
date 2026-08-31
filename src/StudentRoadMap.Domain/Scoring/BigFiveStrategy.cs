using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// Big Five / OCEAN (`BIG5`) — `docs/03-psixologik-metodikalar.md` §3.
/// `MaturityIndex` bu yerda hisoblanmaydi (`ACTIVITY` natijasiga bog'liq) —
/// `CompositeScorer.ApplyMaturityIndex` orqali qo'shiladi.
/// </summary>
public sealed class BigFiveStrategy : IScoringStrategy
{
    private static readonly string[] FactorOrder = ["O", "C", "E", "A", "N"];

    public string StrategyCode => "BIG5";

    public ScoringResult Score(ScoringInput input)
    {
        var byFactor = ScoringMath.GroupByScaleOrdered(input.Questions);

        var rawScores = new Dictionary<string, double>(StringComparer.Ordinal);
        var normalizedScores = new Dictionary<string, double>(StringComparer.Ordinal);
        var levels = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var factor in FactorOrder)
        {
            if (!byFactor.TryGetValue(factor, out var factorQuestions) || factorQuestions.Count == 0)
            {
                throw new DomainException("SCORING_SCALE_EMPTY", $"'{factor}' omili uchun savol topilmadi.");
            }

            // `factorPct` formulasi (10..50 xom ball) aynan 10 savolga mo'ljallangan konstanta
            // (`docs/03` §3.1: "50 savol, 5 omil × 10 savol") — boshqa son bilan `(raw-10)/40*100`
            // natijasi 0..100 tashqarisiga chiqadi, shuning uchun soni qat'iy tekshiriladi.
            if (factorQuestions.Count != ScoringConstants.Big5QuestionsPerFactor)
            {
                throw new DomainException(
                    "SCORING_SCALE_QUESTION_COUNT_MISMATCH",
                    $"'{factor}' omilida {ScoringConstants.Big5QuestionsPerFactor} ta savol bo'lishi kerak, topildi: {factorQuestions.Count}.");
            }

            double factorRaw = 0;
            foreach (var question in factorQuestions)
            {
                ScoringMath.EnsureUnitWeight(question);
                var value = ScoringMath.GetRequiredAnswer(input.Answers, question, ScoringConstants.Likert5Min, ScoringConstants.Likert5Max);
                factorRaw += ScoringMath.ApplyDirection(value, question.Direction, ScoringConstants.Likert5Min, ScoringConstants.Likert5Max);
            }

            var factorPctRaw = (factorRaw - ScoringConstants.Big5FactorRawMin) / ScoringConstants.Big5FactorRawRange * 100.0;

            // Daraja tasnifi va saqlanadigan qiymat bitta izchil (yaxlitlangan) sondan olinadi —
            // QA Bloklovchi-1 bilan bir xil IEEE-754 chegara muammosi BIG5 daraja chegaralarida
            // (20/40/60/80) ham bo'lishi mumkin edi.
            var factorPct = ScorePercent.FromClamped(factorPctRaw).Value;

            rawScores[factor] = factorRaw;
            normalizedScores[factor] = factorPct;
            levels[factor] = ClassifyFactorLevel(factorPct);
        }

        // `docs/03` §3.2: "StabilityPct = 100 − N_pct" — hisobotda "emotsional barqarorlik" sifatida
        // ko'rsatiladi, o'zi mustaqil xom ballga ega emas (shuning uchun faqat NormalizedScores'da).
        var stabilityPct = ScorePercent.FromClamped(100.0 - normalizedScores["N"]).Value;
        normalizedScores["STABILITY"] = stabilityPct;

        return new ScoringResult(
            ResultCode: null,
            rawScores,
            normalizedScores,
            levels,
            CompositeIndex: null,
            Flags: [],
            InterpretationKey: "BIG5.RESULT",
            ScoringVersion: ScoringConstants.CurrentScoringVersion);
    }

    private static string ClassifyFactorLevel(double pct) => pct switch
    {
        <= ScoringConstants.Big5LevelVeryLowMax => ScoringConstants.Big5LevelVeryLow,
        <= ScoringConstants.Big5LevelLowMax => ScoringConstants.Big5LevelLow,
        <= ScoringConstants.Big5LevelAverageMax => ScoringConstants.Big5LevelAverage,
        <= ScoringConstants.Big5LevelHighMax => ScoringConstants.Big5LevelHigh,
        _ => ScoringConstants.Big5LevelVeryHigh,
    };
}
