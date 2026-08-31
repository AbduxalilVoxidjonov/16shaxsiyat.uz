using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// Superadmin `Custom` anketalari uchun universal ball hisobi (`SUM`) — ADR-13,
/// `docs/03-psixologik-metodikalar.md` §6.2. Ilmiy metodikalardan farqli o'laroq shkalalar
/// va talqin oraliqlari testga xos konfiguratsiya (`ScoringInput.ScaleBands`) orqali keladi.
/// </summary>
public sealed class SumStrategy : IScoringStrategy
{
    public string StrategyCode => "SUM";

    public ScoringResult Score(ScoringInput input)
    {
        var byScale = ScoringMath.GroupByScaleOrdered(input.Questions);
        var scaleCodes = byScale.Keys.OrderBy(code => code, StringComparer.Ordinal).ToList();

        if (scaleCodes.Count == 0)
        {
            throw new DomainException("SCORING_SCALE_EMPTY", "'SUM' strategiyasi uchun kamida bitta shkala bo'lishi kerak.");
        }

        var rawScores = new Dictionary<string, double>(StringComparer.Ordinal);
        var normalizedScores = new Dictionary<string, double>(StringComparer.Ordinal);
        var levels = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var scale in scaleCodes)
        {
            var questions = byScale[scale];

            // `docs/03` §6.3 nashr validatsiyasi: "har shkalada kamida 4 savol" — Application
            // qatlamidagi `Publish` tekshiruvi buni oldindan ta'minlashi kerak, lekin Scoring
            // o'ziga ishonib qolmaydi (masalan, eski nashr qilingan anketa qoidasi keyinroq
            // qattiqlashtirilgan bo'lishi mumkin).
            if (questions.Count < ScoringConstants.SumMinQuestionsPerScale)
            {
                throw new DomainException(
                    "SUM_SCALE_TOO_FEW_QUESTIONS",
                    $"'{scale}' shkalasida kamida {ScoringConstants.SumMinQuestionsPerScale} ta savol bo'lishi kerak, topildi: {questions.Count}.");
            }

            decimal scaleRaw = 0;
            decimal scaleMin = 0;
            decimal scaleMax = 0;

            foreach (var question in questions)
            {
                var (min, max) = ScoringMath.GetLikertBounds(question.QuestionType);
                var value = ScoringMath.GetRequiredAnswer(input.Answers, question, min, max);
                var directed = ScoringMath.ApplyDirection(value, question.Direction, min, max);

                scaleRaw += directed * question.Weight;
                scaleMin += min * question.Weight;
                scaleMax += max * question.Weight;
            }

            // Shkaladagi barcha savol og'irligi 0 bo'lsa `scaleMax == scaleMin` (ikkalasi ham 0)
            // bo'lib, keyingi `decimal` bo'lish nolga bo'lishga aylanib qoladi (`DivideByZeroException`,
            // `code`siz 500 — CLAUDE.md 11-qoida buziladi). Oldindan aniq xato bilan to'xtatiladi.
            if (scaleMax == scaleMin)
            {
                throw new DomainException(
                    "SUM_SCALE_WEIGHT_ZERO",
                    $"'{scale}' shkalasidagi barcha savollarning og'irligi 0 — normalizatsiya (bo'lish) mumkin emas.");
            }

            var scalePctRaw = (double)((scaleRaw - scaleMin) / (scaleMax - scaleMin)) * 100.0;

            // Talqin oralig'i tanlovi ham yaxlitlangan qiymat bo'yicha qilinadi — oraliqlar
            // (masalan `[0,33]`/`[34,66]`) chegarasida xuddi QA Bloklovchi-1dagi kabi IEEE-754
            // xatosi bo'lishi mumkin edi.
            var scalePct = ScorePercent.FromClamped(scalePctRaw).Value;

            rawScores[scale] = (double)scaleRaw;
            normalizedScores[scale] = scalePct;
            levels[scale] = ResolveLevel(input.ScaleBands, scale, scalePct);
        }

        return new ScoringResult(
            ResultCode: null,
            rawScores,
            normalizedScores,
            levels,
            CompositeIndex: null,
            Flags: [],
            InterpretationKey: "SUM.RESULT",
            ScoringVersion: ScoringConstants.CurrentScoringVersion);
    }

    private static string ResolveLevel(
        IReadOnlyDictionary<string, IReadOnlyList<InterpretationBand>>? scaleBands,
        string scale,
        double scalePct)
    {
        if (scaleBands is null || !scaleBands.TryGetValue(scale, out var bands) || bands.Count == 0)
        {
            throw new DomainException(
                "SUM_INTERPRETATION_BANDS_MISSING",
                $"'{scale}' shkalasi uchun talqin oraliqlari (`InterpretationBands`) berilmagan.");
        }

        foreach (var band in bands)
        {
            if (scalePct >= band.MinInclusive && scalePct <= band.MaxInclusive)
            {
                return band.Level;
            }
        }

        throw new DomainException(
            "SUM_INTERPRETATION_BAND_NOT_FOUND",
            $"'{scale}' shkalasidagi {scalePct:0.##} foizga mos talqin oralig'i topilmadi.");
    }
}
