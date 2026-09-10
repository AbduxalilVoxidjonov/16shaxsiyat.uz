using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Catalog.Branching;
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

        // `docs/18` §5 — bo'lim/tarmoqlanish qoidalari. B-1/B-2 (`QUESTION_TYPE_NOT_SCORABLE`/
        // `BRANCHING_NOT_ALLOWED_IN_SCORED`) bu yerga QO'SHILMAGAN: ular `TestDefinition.AddQuestion`/
        // `AddSection` orqali SAQLASH vaqtida darhol (`400`) rad etiladi — bunday holat
        // Draft/Published farqisiz umuman bazaga yozilmaydi, shu sabab bu yerda qayta
        // tekshirish imkonsiz holatni "tekshirish" bo'lardi.
        ValidateBranching(test, issues);

        return issues;
    }

    /// <summary>`docs/18` §5 — bo'limlar va ko'rsatish shartlari (`VisibilityRule`) uchun nashr vaqtidagi tekshiruvlar.</summary>
    private static void ValidateBranching(TestDefinition test, List<PublishIssueDto> issues)
    {
        var questionsByCode = test.Questions.ToDictionary(q => q.Code, StringComparer.Ordinal);
        var questionsBySection = test.Questions
            .Where(q => q.SectionId is not null)
            .GroupBy(q => q.SectionId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var question in test.Questions)
        {
            if (question.VisibilityRule is { } rule)
            {
                ValidateVisibilityRule(rule, question.DisplayOrder, question.Code, null, questionsByCode, issues);
            }
        }

        foreach (var section in test.Sections)
        {
            var sectionQuestions = questionsBySection.GetValueOrDefault(section.Id) ?? [];

            if (sectionQuestions.All(q => !q.IsActive))
            {
                issues.Add(new PublishIssueDto("SECTION_EMPTY", null, null, $"'{section.Code}' bo'limida faol savol yo'q.", section.Code));
            }

            if (section.VisibilityRule is { } sectionRule)
            {
                var minOrder = sectionQuestions.Count > 0 ? sectionQuestions.Min(q => q.DisplayOrder) : int.MaxValue;
                ValidateVisibilityRule(sectionRule, minOrder, null, section.Code, questionsByCode, issues);
            }
        }

        foreach (var question in test.Questions.Where(q => q.IsActive))
        {
            if (question.QuestionType is QuestionType.SingleChoice or QuestionType.MultiChoice && question.Options.Count < 2)
            {
                issues.Add(new PublishIssueDto("QUESTION_OPTIONS_REQUIRED", null, question.Code, $"'{question.Code}' savolida kamida 2 ta variant bo'lishi kerak."));
            }

            if (question.Options.GroupBy(o => o.Value).Any(g => g.Count() > 1))
            {
                issues.Add(new PublishIssueDto("QUESTION_OPTION_VALUE_DUPLICATE", null, question.Code, $"'{question.Code}' savolida takroriy variant qiymati bor."));
            }

            if (question.InputPattern is { Length: > 0 } pattern && !CachedInputPatternMatcher.IsValidPattern(pattern))
            {
                issues.Add(new PublishIssueDto("INPUT_PATTERN_INVALID", null, question.Code, $"'{question.Code}' savolining shablon (InputPattern) noto'g'ri."));
            }
        }
    }

    /// <summary>
    /// Bitta shart (bo'lim yoki savol darajasidagi) — `docs/18` §5: `VISIBILITY_UNKNOWN_QUESTION`,
    /// `VISIBILITY_FORWARD_REFERENCE` (B-4 — faqat oldingi savolga), `VISIBILITY_OPERATOR_MISMATCH`,
    /// `VISIBILITY_VALUE_UNKNOWN`. <paramref name="referencingOrder"/> — savol uchun o'sha
    /// savolning `DisplayOrder`i, bo'lim uchun bo'limdagi ENG KICHIK savol tartibi.
    /// </summary>
    private static void ValidateVisibilityRule(
        VisibilityRule rule,
        int referencingOrder,
        string? questionCode,
        string? sectionCode,
        IReadOnlyDictionary<string, Question> questionsByCode,
        List<PublishIssueDto> issues)
    {
        foreach (var condition in rule.Conditions)
        {
            if (!questionsByCode.TryGetValue(condition.QuestionCode, out var source))
            {
                issues.Add(new PublishIssueDto("VISIBILITY_UNKNOWN_QUESTION", null, questionCode, $"'{condition.QuestionCode}' kodli savol topilmadi.", sectionCode));
                continue;
            }

            if (source.DisplayOrder >= referencingOrder)
            {
                issues.Add(new PublishIssueDto("VISIBILITY_FORWARD_REFERENCE", null, questionCode, $"Shart '{condition.QuestionCode}' savoliga (keyingi/joriy tartibdagi) havola qiladi — faqat oldingi savolga ruxsat (B-4).", sectionCode));
            }

            var isMismatch = condition.Operator switch
            {
                VisibilityOperator.ContainsAny or VisibilityOperator.ContainsAll => source.QuestionType != QuestionType.MultiChoice,
                VisibilityOperator.Equals or VisibilityOperator.NotEquals or VisibilityOperator.AnyOf or VisibilityOperator.NoneOf => !IsValueType(source.QuestionType),
                _ => false,
            };

            if (isMismatch)
            {
                issues.Add(new PublishIssueDto("VISIBILITY_OPERATOR_MISMATCH", null, questionCode, $"'{condition.Operator}' operatori '{condition.QuestionCode}' ({source.QuestionType}) savol turiga mos emas.", sectionCode));
                continue;
            }

            if (condition.Operator is VisibilityOperator.Answered or VisibilityOperator.NotAnswered)
            {
                continue;
            }

            var validValues = ResolveValidValues(source);
            if (validValues is not null && condition.Values.Any(v => !validValues.Contains(v)))
            {
                issues.Add(new PublishIssueDto("VISIBILITY_VALUE_UNKNOWN", null, questionCode, $"'{condition.QuestionCode}' savolining variantlari/darajalari orasida ko'rsatilgan qiymat yo'q.", sectionCode));
            }
        }
    }

    /// <summary>`Equals`/`NotEquals`/`AnyOf`/`NoneOf` faqat qiymatli turlarga (`docs/18` §5) — matn turlariga (`ShortText`/`LongText`/`Phone`) taqiqlangan.</summary>
    private static bool IsValueType(QuestionType type) =>
        type is QuestionType.Likert5 or QuestionType.Likert7 or QuestionType.Binary
            or QuestionType.SingleChoice or QuestionType.ForcedChoice or QuestionType.MultiChoice;

    /// <summary>Manba savolning haqiqiy qiymatlar to'plami — `VISIBILITY_VALUE_UNKNOWN` uchun. Matn turlari uchun `null` (tekshirilmaydi — operator mos kelmasligi allaqachon ushlangan).</summary>
    private static HashSet<int>? ResolveValidValues(Question source) => source.QuestionType switch
    {
        QuestionType.Likert5 => [1, 2, 3, 4, 5],
        QuestionType.Likert7 => [1, 2, 3, 4, 5, 6, 7],
        QuestionType.Binary => [0, 1],
        QuestionType.SingleChoice or QuestionType.ForcedChoice or QuestionType.MultiChoice => source.Options.Select(o => o.Value).ToHashSet(),
        _ => null,
    };

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
