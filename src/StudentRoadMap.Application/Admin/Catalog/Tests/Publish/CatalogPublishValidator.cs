using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Publish;

/// <summary>
/// `docs/03` §6.3 nashr validatsiyasi — `TestDefinition.Publish()` domen qo'riqchisi faqat BITTA
/// sababni (birinchi topilganini) istisno bilan bildiradi, lekin `docs/07` §3.4 "xatolar RO'YXAT
/// sifatida qaytadi" talab qiladi. Shu sabab bu qoidalar Application qatlamida, `Publish()`dan
/// OLDIN, TO'LIQ ro'yxat sifatida hisoblanadi — bo'sh bo'lsa (issues.Count == 0) handler domen
/// `Publish()`ni chaqiradi.
/// </summary>
internal static class CatalogPublishValidator
{
    public static IReadOnlyList<PublishIssueDto> Validate(TestDefinition test)
    {
        var issues = new List<PublishIssueDto>();
        var activeQuestions = test.Questions.Where(q => q.IsActive).ToList();

        if (activeQuestions.Count == 0)
        {
            issues.Add(new PublishIssueDto("TEST_HAS_NO_QUESTIONS", null, null, "Kamida bitta faol savol bo'lishi kerak."));
        }

        // `docs/03` §6.3 shkala qoidalari faqat `SUM` (superadmin `Custom` anketasi) uchun —
        // `Survey` rejimida va ilmiy (`Standard`) metodikalarda shkalalar bu API orqali
        // boshqarilmaydi (`docs/07` §3.4: "Shkalalar — faqat Custom").
        if (test.ScoringMode == TestScoringMode.Scored && string.Equals(test.ScoringStrategyCode, "SUM", StringComparison.Ordinal))
        {
            if (test.Scales.Count == 0)
            {
                issues.Add(new PublishIssueDto("TEST_HAS_NO_SCALES", null, null, "Kamida bitta shkala bo'lishi kerak."));
            }

            var scaleCodes = test.Scales.Select(s => s.Code).ToHashSet(StringComparer.Ordinal);

            foreach (var question in activeQuestions)
            {
                if (!scaleCodes.Contains(question.Scale))
                {
                    issues.Add(new PublishIssueDto("QUESTION_WITHOUT_SCALE", null, question.Code, $"'{question.Code}' savoli uchun shkala tanlanmagan."));
                }
            }

            foreach (var scale in test.Scales)
            {
                var questionCount = activeQuestions.Count(q => q.Scale == scale.Code);
                if (questionCount < ScoringConstants.SumMinQuestionsPerScale)
                {
                    issues.Add(new PublishIssueDto(
                        "SCALE_TOO_FEW_QUESTIONS",
                        scale.Code,
                        null,
                        $"Kamida {ScoringConstants.SumMinQuestionsPerScale} savol kerak, hozir {questionCount}"));
                }

                ValidateBandCoverage(scale, issues);
            }
        }

        return issues;
    }

    /// <summary>
    /// Talqin oraliqlari — PM qarori (2026-09-02, `docs/03` §6.3 ga yozilmoqda): TOLERANTLIK
    /// o'rniga QAT'IY BUTUN SON qoidasi. Sabab: superadmin oraliqni qo'lda kiritadi — kasrli
    /// chegara ruxsat etilsa (masalan `0–33.3`/`33.4–66.6`), `33.35` ballga ega o'quvchi HECH
    /// QAYSI oralig'iga tushmaydi va `SumStrategy` `SUM_INTERPRETATION_BAND_NOT_FOUND` bilan
    /// yiqiladi (P09 QA qarzi) — tolerantlik bu holatni yashiradi, yo'qotmaydi. Qoida:
    /// 1) har bir chegara (`from`/`to`) BUTUN SON; 2) `to` INKLYUZIV; 3) ketma-ket oraliqlar
    /// uchun `next.from == prev.to + 1`; 4) birinchi oraliq `from == 0`, oxirgisi `to == 100`.
    /// </summary>
    private static void ValidateBandCoverage(TestScale scale, List<PublishIssueDto> issues)
    {
        if (scale.InterpretationBands.Count == 0)
        {
            issues.Add(new PublishIssueDto("SCALE_BANDS_MISSING", scale.Code, null, "Talqin oraliqlari belgilanmagan."));
            return;
        }

        if (scale.InterpretationBands.Any(b => !IsWholeNumber(b.MinInclusive) || !IsWholeNumber(b.MaxInclusive)))
        {
            // Keyingi (bo'shliq/ustma-ust/to'liqlik) tekshiruvlar kasrli chegaralar bilan
            // ma'nosiz — bitta aniq xato yetarli, chalkash qo'shimcha xabar berilmaydi.
            issues.Add(new PublishIssueDto("SCALE_BAND_NOT_INTEGER", scale.Code, null, "Talqin oralig'i chegaralari butun son bo'lishi shart."));
            return;
        }

        var ordered = scale.InterpretationBands.OrderBy(b => b.MinInclusive).ToList();

        var hasInvalidBand = false;
        foreach (var band in ordered)
        {
            if (band.MinInclusive > band.MaxInclusive)
            {
                issues.Add(new PublishIssueDto("SCALE_BAND_INVALID", scale.Code, null, $"'{band.Level}' oralig'ida minimal qiymat maksimaldan katta."));
                hasInvalidBand = true;
            }
        }

        if (hasInvalidBand)
        {
            return;
        }

        if (ordered[0].MinInclusive != InterpretationBandRangeMin || ordered[^1].MaxInclusive != InterpretationBandRangeMax)
        {
            issues.Add(new PublishIssueDto("SCALE_BAND_INCOMPLETE", scale.Code, null, "Talqin oraliqlari 0 dan boshlanib 100 da tugashi shart."));
        }

        for (var i = 1; i < ordered.Count; i++)
        {
            var gap = ordered[i].MinInclusive - ordered[i - 1].MaxInclusive;
            if (gap > 1)
            {
                issues.Add(new PublishIssueDto("SCALE_BAND_GAP", scale.Code, null, "Talqin oraliqlarida bo'shliq bor."));
            }
            else if (gap < 1)
            {
                issues.Add(new PublishIssueDto("SCALE_BAND_OVERLAP", scale.Code, null, "Talqin oraliqlari ustma-ust tushadi."));
            }
        }
    }

    /// <summary>`value` butun sonmi (`InterpretationBand.MinInclusive`/`MaxInclusive` `double`, lekin superadmin kiritishi butun bo'lishi shart — QP raqamlar aniqligi uchun kichik epsilon bilan).</summary>
    private static bool IsWholeNumber(double value) => Math.Abs(value - Math.Round(value)) < 1e-9;

    private const double InterpretationBandRangeMin = ScorePercent.MinValue;

    private const double InterpretationBandRangeMax = ScorePercent.MaxValue;
}
