using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// 16 tipli shaxsiyat modeli (`MBTI16`) — `docs/03-psixologik-metodikalar.md` §2.
/// 4 dixotomiya (`EI`, `SN`, `TF`, `JP`), har biri mustaqil hisoblanadi.
/// </summary>
public sealed class Mbti16Strategy : IScoringStrategy
{
    /// <summary>O'q tartibi — `ResultCode` shu tartibda tuziladi (`docs/03` §2.3 misoli: `INTJ`).</summary>
    private static readonly string[] AxisOrder = ["EI", "SN", "TF", "JP"];

    /// <summary>Har o'q uchun (0-tomon harfi, 100-tomon harfi, tie-break harfi) — `docs/03` §2.2.</summary>
    private static readonly Dictionary<string, (char Pole0, char Pole100, char TieBreak)> AxisLetters = new(StringComparer.Ordinal)
    {
        ["EI"] = ('I', 'E', 'I'),
        ["SN"] = ('S', 'N', 'S'),
        ["TF"] = ('T', 'F', 'T'),
        ["JP"] = ('P', 'J', 'J'),
    };

    public string StrategyCode => "MBTI16";

    public ScoringResult Score(ScoringInput input)
    {
        var byAxis = ScoringMath.GroupByScaleOrdered(input.Questions);

        var rawScores = new Dictionary<string, double>(StringComparer.Ordinal);
        var normalizedScores = new Dictionary<string, double>(StringComparer.Ordinal);
        var levels = new Dictionary<string, string>(StringComparer.Ordinal);
        var flags = new List<string>();
        var letters = new char[AxisOrder.Length];

        for (var i = 0; i < AxisOrder.Length; i++)
        {
            var axis = AxisOrder[i];
            if (!byAxis.TryGetValue(axis, out var axisQuestions) || axisQuestions.Count == 0)
            {
                throw new DomainException("SCORING_SCALE_EMPTY", $"'{axis}' o'qi uchun savol topilmadi.");
            }

            double axisRaw = 0;
            double axisMin = 0;
            double axisMax = 0;

            foreach (var question in axisQuestions)
            {
                ScoringMath.EnsureUnitWeight(question);
                var value = ScoringMath.GetRequiredAnswer(input.Answers, question, ScoringConstants.Likert5Min, ScoringConstants.Likert5Max);
                axisRaw += question.Direction * value;
                axisMin += question.Direction > 0 ? ScoringConstants.Likert5Min : -ScoringConstants.Likert5Max;
                axisMax += question.Direction > 0 ? ScoringConstants.Likert5Max : -ScoringConstants.Likert5Min;
            }

            var axisPctRaw = (axisRaw - axisMin) / (axisMax - axisMin) * 100.0;

            // QA BLOKLOVCHI 1: chegara (45/50/55) tekshiruvi YAXLITLANGAN qiymat bo'yicha
            // qilinadi — IEEE-754 arifmetikasida `axisPctRaw` haqiqiy `55.0` o'rniga
            // `55.00000000000001` bo'lib chiqishi mumkin, bu holda `<= 55.0` yolg'on bo'lib,
            // borderline bayrog'i (va harf tanlovi) noto'g'ri chiqadi. Harf/bayroq/saqlash —
            // barchasi bitta izchil (yaxlitlangan) qiymatdan hisoblanadi.
            var axisPct = ScorePercent.FromClamped(axisPctRaw).Value;
            var (pole0, pole100, tieBreak) = AxisLetters[axis];

            char letter;
            if (axisPct > ScoringConstants.TieBreakPct)
            {
                letter = pole100;
            }
            else if (axisPct < ScoringConstants.TieBreakPct)
            {
                letter = pole0;
            }
            else
            {
                letter = tieBreak;
            }

            if (axisPct is >= ScoringConstants.BorderlineZoneMin and <= ScoringConstants.BorderlineZoneMax)
            {
                flags.Add($"Borderline:{axis}");
            }

            letters[i] = letter;
            rawScores[axis] = axisRaw;
            normalizedScores[axis] = axisPct;
            levels[axis] = letter.ToString();
        }

        var resultCode = PersonalityType.Create(letters[0], letters[1], letters[2], letters[3]);

        return new ScoringResult(
            resultCode.Code,
            rawScores,
            normalizedScores,
            levels,
            CompositeIndex: null,
            Flags: flags,
            InterpretationKey: $"MBTI16.{resultCode.Code}",
            ScoringVersion: ScoringConstants.CurrentScoringVersion);
    }
}
