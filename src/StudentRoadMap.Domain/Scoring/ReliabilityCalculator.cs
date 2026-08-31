using StudentRoadMap.Domain.Assessments;

namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// Ishonchlilik indeksi (`ReliabilityScore`) — `docs/03-psixologik-metodikalar.md` §7,
/// barcha 5 jarima signali. Sof funksiya: DB, IO, tizim soati yo'q.
/// </summary>
public static class ReliabilityCalculator
{
    /// <summary>Jarima sabablari — sobit chiqish tartibi (determinizm uchun).</summary>
    private const string ReasonAllSameAnswer = "AllSameAnswer";
    private const string ReasonStraightLining = "StraightLining";
    private const string ReasonFastAnswers = "FastAnswers";
    private const string ReasonReverseConflict = "ReverseConflict";
    private const string ReasonShortSession = "ShortSession";

    public static ReliabilityResult Calculate(ReliabilityInput input)
    {
        var penalties = new List<(string Reason, double Penalty)>();

        // QA Bloklovchi-2 tuzatmasi: `input.Questions` allaqachon o'quvchining xronologik javob
        // berish tartibida (`ReliabilityInput.Questions` izohiga qarang) — bu yerda QAYTA
        // TARTIBLANMAYDI. Avvalgi versiya `DisplayOrder` bo'yicha saralar edi, bu 4 ta test
        // blokini (har biri o'z ichida `1..N` tartibida) bir-biriga aralashtirib yuborardi.
        var orderedAnswered = input.Questions
            .Where(q => input.Answers.ContainsKey(q.QuestionId))
            .Select(q => input.Answers[q.QuestionId])
            .ToList();

        // "To'liq bir xil javob" (docs/03 §7) — barcha javoblar (qaysi qiymat bo'lishidan qat'i
        // nazar) bir xil bo'lsa qattiq jarima; bu holat straight-lining'ning "kuchliroq" alohida
        // holati bo'lgani uchun ikkalasi birga qo'shilmaydi (PM tasdiqlagan, `docs/03` §7ga
        // aniq yoziladi: "hammasi bir xil → jarima 50", ikkalasi qo'shilsa 80 bo'lib qolar edi).
        var allSame = orderedAnswered.Count > 0 && orderedAnswered.All(v => v == orderedAnswered[0]);
        if (allSame)
        {
            penalties.Add((ReasonAllSameAnswer, ScoringConstants.AllSameAnswerPenalty));
        }
        else
        {
            var straightLiningPenalty = CalculateStraightLiningPenalty(orderedAnswered);
            if (straightLiningPenalty > 0)
            {
                penalties.Add((ReasonStraightLining, straightLiningPenalty));
            }
        }

        var fastAnswerPenalty = CalculateFastAnswerPenalty(input.Answers, input.Durations);
        if (fastAnswerPenalty > 0)
        {
            penalties.Add((ReasonFastAnswers, fastAnswerPenalty));
        }

        var reverseConflictPenalty = CalculateReverseConflictPenalty(input.Questions, input.Answers);
        if (reverseConflictPenalty > 0)
        {
            penalties.Add((ReasonReverseConflict, reverseConflictPenalty));
        }

        if (input.TotalSessionDuration.TotalMinutes < ScoringConstants.ShortSessionThresholdMinutes)
        {
            penalties.Add((ReasonShortSession, ScoringConstants.ShortSessionPenalty));
        }

        var totalPenalty = penalties.Sum(p => p.Penalty);
        var score = ScorePercent.FromClamped(100.0 - totalPenalty).Value;

        var flag = score switch
        {
            >= ScoringConstants.ReliabilityReliableMin => ReliabilityFlag.Reliable,
            >= ScoringConstants.ReliabilityQuestionableMin => ReliabilityFlag.Questionable,
            _ => ReliabilityFlag.Unreliable,
        };

        var reasons = penalties.Select(p => p.Reason).ToList();

        return new ReliabilityResult(score, flag, reasons);
    }

    /// <summary>
    /// Ketma-ket bir xil qiymatlardan iborat har bir TO'LIQ 12talik seriya alohida blok
    /// hisoblanadi (PM qarori: "36 → 3 blok → 30", eski talqin uzun seriyani bitta blok
    /// sifatida sanab jarimani kamsitardi). Har blok uchun 10, umumiy chegara 30 (`docs/03` §7).
    /// </summary>
    private static double CalculateStraightLiningPenalty(IReadOnlyList<int> orderedValues)
    {
        if (orderedValues.Count == 0)
        {
            return 0;
        }

        var blockCount = 0;
        var runLength = 1;

        for (var i = 1; i <= orderedValues.Count; i++)
        {
            var continuesRun = i < orderedValues.Count && orderedValues[i] == orderedValues[i - 1];
            if (continuesRun)
            {
                runLength++;
                continue;
            }

            blockCount += runLength / ScoringConstants.StraightLiningMinRunLength;
            runLength = 1;
        }

        return Math.Min(ScoringConstants.StraightLiningPenaltyCap, blockCount * ScoringConstants.StraightLiningPenaltyPerBlock);
    }

    /// <summary>
    /// `DurationMs &lt; 900` bo'lgan javoblar ulushi `p` uchun `min(40, p × 100 × 0.8)` (`docs/03`
    /// §7). QA M4 tuzatmasi: maxraj **javob berilgan savollar soni** (`answers.Count`) — avvalgi
    /// versiyada maxraj `durations.Count` edi, klient qisman `duration` yuborganda (masalan 20
    /// javobdan faqat 1 tasining vaqti kelsa) `p` sun'iy oshib ketardi.
    /// </summary>
    private static double CalculateFastAnswerPenalty(IReadOnlyDictionary<Guid, int> answers, IReadOnlyDictionary<Guid, int> durations)
    {
        if (answers.Count == 0)
        {
            return 0;
        }

        var fastCount = answers.Keys.Count(id => durations.TryGetValue(id, out var ms) && ms < ScoringConstants.FastAnswerDurationThresholdMs);
        var p = (double)fastCount / answers.Count;

        return Math.Min(ScoringConstants.FastAnswerPenaltyCap, p * 100.0 * ScoringConstants.FastAnswerPenaltyMultiplier);
    }

    /// <summary>
    /// `+1` va `-1` yo'nalishli savollar juftligida o'rtacha mos kelmaslik `d` (0..1), `d × 25`
    /// (`docs/03` §7, PM tasdiqlagan talqin: shkala darajasida, musbatlarning normallashgan
    /// o'rtachasi ↔ teskari tuzatilgan manfiylarning o'rtachasi). QA M13 tuzatmasi: shkala
    /// ichidagi savollar `ScoringMath.GroupByScaleOrdered` bilan `(DisplayOrder, QuestionId)`
    /// tartibida qayta ishlanadi — strategiyalar bilan izchil, kirish ro'yxati tartibiga
    /// bog'liq emas.
    /// </summary>
    private static double CalculateReverseConflictPenalty(
        IReadOnlyList<QuestionMeta> questions,
        IReadOnlyDictionary<Guid, int> answers)
    {
        var mismatches = new List<double>();

        foreach (var scaleQuestions in ScoringMath.GroupByScaleOrdered(questions).Values)
        {
            var positives = new List<double>();
            var negativesCorrected = new List<double>();

            foreach (var question in scaleQuestions)
            {
                if (!answers.TryGetValue(question.QuestionId, out var value))
                {
                    continue;
                }

                var (min, max) = ScoringMath.GetLikertBounds(question.QuestionType);
                // `GetLikertBounds` doim ijobiy diapazon qaytaradi (Likert5: 4, Likert7: 6) —
                // nol bilan bo'lish holati mumkin emas, shuning uchun himoya tekshiruvi yo'q.
                var range = max - min;

                if (question.Direction > 0)
                {
                    positives.Add((value - min) / (double)range);
                }
                else
                {
                    var corrected = max + min - value;
                    negativesCorrected.Add((corrected - min) / (double)range);
                }
            }

            if (positives.Count > 0 && negativesCorrected.Count > 0)
            {
                mismatches.Add(Math.Abs(positives.Average() - negativesCorrected.Average()));
            }
        }

        var d = mismatches.Count > 0 ? mismatches.Average() : 0.0;
        return d * ScoringConstants.ReverseConflictPenaltyMultiplier;
    }
}
