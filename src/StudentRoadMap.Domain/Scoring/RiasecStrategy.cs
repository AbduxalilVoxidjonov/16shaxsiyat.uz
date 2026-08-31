using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// Kasb qiziqishlari, RIASEC (`RIASEC`) — `docs/03-psixologik-metodikalar.md` §4.
/// Teskari savol yo'q (barcha `direction = +1`).
/// </summary>
public sealed class RiasecStrategy : IScoringStrategy
{
    /// <summary>Tip tartibi — `Scale` kodlari, tie-break prioriteti aynan shu tartib (`docs/03` §4.2).</summary>
    private static readonly string[] TypeOrder = ["R", "I", "ART", "SOC", "ENT", "CONV"];

    /// <summary>`TypeOrder`ga mos bitta harfli mnemonika (`docs/03` §4.1) — Holland kodi va
    /// olti burchak konsistentligi shu harflar bilan ishlaydi.</summary>
    private static readonly char[] TypeLetters = ['R', 'I', 'A', 'S', 'E', 'C'];

    public string StrategyCode => "RIASEC";

    public ScoringResult Score(ScoringInput input)
    {
        var byType = ScoringMath.GroupByScaleOrdered(input.Questions);

        var rawScores = new Dictionary<string, double>(StringComparer.Ordinal);
        var normalizedScores = new Dictionary<string, double>(StringComparer.Ordinal);
        var typePcts = new double[TypeOrder.Length];

        for (var i = 0; i < TypeOrder.Length; i++)
        {
            var type = TypeOrder[i];
            if (!byType.TryGetValue(type, out var typeQuestions) || typeQuestions.Count == 0)
            {
                throw new DomainException("SCORING_SCALE_EMPTY", $"'{type}' tipi uchun savol topilmadi.");
            }

            // `typePct` formulasi (8..40 xom ball) aynan 8 savolga mo'ljallangan konstanta
            // (`docs/03` §4.1: "48 savol, 6 tip × 8 savol") — boshqa son bilan `(raw-8)/32*100`
            // natijasi 0..100 tashqarisiga chiqadi, shuning uchun soni qat'iy tekshiriladi.
            if (typeQuestions.Count != ScoringConstants.RiasecQuestionsPerType)
            {
                throw new DomainException(
                    "SCORING_SCALE_QUESTION_COUNT_MISMATCH",
                    $"'{type}' tipida {ScoringConstants.RiasecQuestionsPerType} ta savol bo'lishi kerak, topildi: {typeQuestions.Count}.");
            }

            double typeRaw = 0;
            foreach (var question in typeQuestions)
            {
                ScoringMath.EnsureUnitWeight(question);

                // `docs/03` §4.1: "Teskari savol yo'q" — barcha RIASEC savollari `direction=+1`
                // bo'lishi kerak; boshqacha qiymat kelsa jimgina yutish o'rniga xato beriladi.
                if (question.Direction != 1)
                {
                    throw new DomainException(
                        "SCORING_UNEXPECTED_DIRECTION",
                        $"'{question.Code}' — RIASEC'da faqat 'direction=+1' bo'lishi mumkin, berilgan: {question.Direction}.");
                }

                typeRaw += ScoringMath.GetRequiredAnswer(input.Answers, question, ScoringConstants.Likert5Min, ScoringConstants.Likert5Max);
            }

            var typePctRaw = (typeRaw - ScoringConstants.RiasecTypeRawMin) / ScoringConstants.RiasecTypeRawRange * 100.0;

            // Reyting (top-3), differensiatsiya va konsistentlik — barchasi bitta izchil
            // (yaxlitlangan) qiymatdan hisoblanadi (QA Bloklovchi-1 bilan bir xil sabab).
            var typePct = ScorePercent.FromClamped(typePctRaw).Value;

            rawScores[type] = typeRaw;
            normalizedScores[type] = typePct;
            typePcts[i] = typePct;
        }

        // Eng yuqori 3 tip, kamayish tartibida; teng bo'lsa `TypeOrder` prioriteti (`docs/03` §4.2).
        var top3Indexes = Enumerable.Range(0, TypeOrder.Length)
            .OrderByDescending(i => typePcts[i])
            .ThenBy(i => i)
            .Take(3)
            .ToList();

        var hollandCode = HollandCode.Create(top3Indexes.Select(i => TypeLetters[i]).ToList());

        var differentiation = ScorePercent.FromClamped(typePcts.Max() - typePcts.Min()).Value;
        normalizedScores["DIFFERENTIATION"] = differentiation;

        var flags = new List<string>();
        if (differentiation < ScoringConstants.RiasecLowDifferentiationThreshold)
        {
            flags.Add("LowDifferentiation");
        }

        var consistency = ClassifyConsistency(top3Indexes[0], top3Indexes[1]);
        var levels = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CONSISTENCY"] = consistency,
        };

        return new ScoringResult(
            hollandCode.Code,
            rawScores,
            normalizedScores,
            levels,
            CompositeIndex: null,
            Flags: flags,
            InterpretationKey: $"RIASEC.{hollandCode.Code}",
            ScoringVersion: ScoringConstants.CurrentScoringVersion);
    }

    /// <summary>
    /// Holland olti burchagida (`R-I-A-S-E-C` aylana) birinchi ikki harf orasidagi masofa:
    /// qo'shni (1) — `High`, bitta oralatib (2) — `Medium`, qarama-qarshi (3) — `Low` (`docs/03` §4.2).
    /// </summary>
    private static string ClassifyConsistency(int firstIndex, int secondIndex)
    {
        var diff = Math.Abs(firstIndex - secondIndex);
        var circularDistance = Math.Min(diff, TypeOrder.Length - diff);

        return circularDistance switch
        {
            1 => ScoringConstants.RiasecConsistencyHigh,
            2 => ScoringConstants.RiasecConsistencyMedium,
            _ => ScoringConstants.RiasecConsistencyLow,
        };
    }
}
