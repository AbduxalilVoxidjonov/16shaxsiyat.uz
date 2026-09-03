using StudentRoadMap.Application.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Admin.Students;

/// <summary>
/// `TestResult`/`AiAnalysis` (jsonb ustunlar) dan `docs/07-api-shartnoma.md` 3.2-bo'lim
/// `latestAssessment.results`/`aiAnalysis` shakliga o'girish — `GetStudentByIdQueryHandler`
/// uchun. Formulalar/JSON tuzilishi `docs/03-psixologik-metodikalar.md` §2-5 ga mos
/// (`Domain.Scoring` strategiyalari yozgan xom `NormalizedScores`/`Levels`/`Flags` lug'atlarini
/// UI shakliga yig'adi — QAYTA HISOBLAMAYDI, faqat o'qiydi).
///
/// **`scale`/`scaleDirection` bu yerda HECH QACHON chiqarilmaydi** (`CLAUDE.md` 9-band) —
/// faqat shkala KODI (masalan `"EI"`, `"MOT"`) lug'at KALITI sifatida, bu — natija shkalasi
/// identifikatori, savol metama'lumoti EMAS.
/// </summary>
internal static class StudentProfileMapping
{
    private const int MaxCareerFields = 3;

    /// <summary>RIASEC bitta harfli mnemonika (`docs/03` §4.1) → bazadagi `scale` kodi. `RiasecStrategy.TypeLetters`/`TypeOrder` bilan bir xil.</summary>
    private static readonly IReadOnlyDictionary<string, string> RiasecScaleToLetter = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["R"] = "R",
        ["I"] = "I",
        ["ART"] = "A",
        ["SOC"] = "S",
        ["ENT"] = "E",
        ["CONV"] = "C",
    };

    /// <summary>
    /// `docs/07` 3.2 `results` blokini yig'adi.
    ///
    /// <para>
    /// ⚠️ Qaysi natija qaysi blokka tushishi metodika KODI bilan aniqlanmaydi (`docs/06`
    /// 8-bo'lim, 2026-09-02 "dastur" qarori). Ilgari bu yerda `TestCode == "MBTI16"` kabi satr
    /// solishtiruvi turardi va `AssessmentProgram` kiritilgandan keyin JIMGINA buzilardi:
    /// `MBTI16` KODLI `Custom` anketa haqiqiy batareya o'rniga `results.MBTI16` ga tushib
    /// ketardi (yoki `PERS-BAT-1` kabi boshqa kodli HAQIQIY batareya umuman ko'rinmasdi) —
    /// hech qanday xato ko'rsatilmasdi. Mezon — `PersonalityBattery.RoleOf`
    /// (`ScoringStrategyCode`, ya'ni natijani hisoblagan ALGORITM bo'yicha),
    /// `PromptBuilder`/`RecalculateAssessmentScoresCommandHandler` bilan BIR XIL.
    /// </para>
    ///
    /// <para>
    /// **JSON kalitlari o'zgarmaydi** — `MBTI16`/`BIG5`/`RIASEC`/`ACTIVITY`
    /// (`AdminTestResultsDto` dagi `JsonPropertyName`, 2026-09-03 qarori). O'zgargani — faqat
    /// qaysi natija qaysi kalitga tushishining MEZONI.
    /// </para>
    ///
    /// <para>
    /// Rol xaritasi BITTA so'rov bilan yuklanadi (`LoadByAssessmentTestIdAsync`) — N+1 yo'q.
    /// Sessiya identifikatori `TestResult.AssessmentId` dan olinadi: hamma chaqiruvchi bu
    /// ro'yxatni AYNAN bitta sessiya bo'yicha filtrlab beradi, shu sabab imzoni o'zgartirish
    /// (va to'rtta chaqiruvchini tahrirlash) shart emas.
    /// </para>
    /// </summary>
    public static async Task<AdminTestResultsDto> BuildTestResultsAsync(
        IReadOnlyList<TestResult> testResults,
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(testResults);

        if (testResults.Count == 0)
        {
            // Hali birorta natija yo'q — hamma blok `null` (bo'sh obyekt EMAS: "ma'lumot yo'q"
            // va "nol ball" bir xil emas, `docs/06` qarorlar jurnali 2026-09-02).
            return new AdminTestResultsDto(null, null, null, null);
        }

        var rolesByAssessmentTestId = await PersonalityBatteryRoles
            .LoadByAssessmentTestIdAsync(context, executor, testResults[0].AssessmentId, cancellationToken)
            .ConfigureAwait(false);

        // Batareyasiz dasturda (yoki roli tanilmagan `Custom` anketalarda) xarita bo'sh bo'ladi
        // va tegishli blok `null` qaytadi.
        var mbti = PersonalityBatteryRoles.FindByRole(testResults, rolesByAssessmentTestId, PersonalityBatteryRole.PersonalityType);
        var bigFive = PersonalityBatteryRoles.FindByRole(testResults, rolesByAssessmentTestId, PersonalityBatteryRole.Traits);
        var riasec = PersonalityBatteryRoles.FindByRole(testResults, rolesByAssessmentTestId, PersonalityBatteryRole.CareerInterest);
        var activity = PersonalityBatteryRoles.FindByRole(testResults, rolesByAssessmentTestId, PersonalityBatteryRole.Activity);

        return new AdminTestResultsDto(
            mbti is null ? null : await BuildMbti16Async(mbti, context, executor, cancellationToken).ConfigureAwait(false),
            bigFive is null ? null : BuildBig5(bigFive),
            riasec is null ? null : await BuildRiasecAsync(riasec, context, executor, cancellationToken).ConfigureAwait(false),
            activity is null ? null : BuildActivity(activity));
    }

    private static async Task<AdminMbti16Dto> BuildMbti16Async(
        TestResult result,
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        CancellationToken cancellationToken)
    {
        var normalized = TestResultJson.DeserializeScores(result.NormalizedScoresJson);
        var levels = TestResultJson.DeserializeLevels(result.LevelsJson);
        var flags = TestResultJson.DeserializeFlags(result.FlagsJson);

        var axes = new Dictionary<string, AdminAxisDto>(StringComparer.Ordinal);
        var borderlineAxes = new List<string>();
        foreach (var axis in new[] { "EI", "SN", "TF", "JP" })
        {
            if (!normalized.TryGetValue(axis, out var pct) || !levels.TryGetValue(axis, out var letter))
            {
                continue;
            }

            var borderline = flags.Contains($"Borderline:{axis}");
            axes[axis] = new AdminAxisDto(pct, letter, borderline);
            if (borderline)
            {
                borderlineAxes.Add(axis);
            }
        }

        var typeName = string.Empty;
        if (!string.IsNullOrEmpty(result.ResultCode))
        {
            var typeCatalogEntry = await executor.FirstOrDefaultAsync(
                context.AsNoTracking(context.TypeCatalog).Where(t => t.Code == result.ResultCode),
                cancellationToken).ConfigureAwait(false);
            typeName = typeCatalogEntry?.NameUz ?? string.Empty;
        }

        return new AdminMbti16Dto(result.ResultCode ?? string.Empty, typeName, axes, borderlineAxes);
    }

    private static AdminBig5Dto BuildBig5(TestResult result)
    {
        var raw = TestResultJson.DeserializeScores(result.RawScoresJson);
        var normalized = TestResultJson.DeserializeScores(result.NormalizedScoresJson);
        var levels = TestResultJson.DeserializeLevels(result.LevelsJson);

        var factors = new Dictionary<string, AdminFactorDto>(StringComparer.Ordinal);
        foreach (var factor in new[] { "O", "C", "E", "A", "N" })
        {
            if (!raw.TryGetValue(factor, out var rawValue) || !normalized.TryGetValue(factor, out var pct) || !levels.TryGetValue(factor, out var level))
            {
                continue;
            }

            factors[factor] = new AdminFactorDto(rawValue, pct, level);
        }

        var stabilityPct = normalized.GetValueOrDefault("STABILITY");
        var maturityLevel = levels.GetValueOrDefault("MATURITY");

        return new AdminBig5Dto(factors, stabilityPct, result.CompositeIndex, string.IsNullOrEmpty(maturityLevel) ? null : maturityLevel);
    }

    private static async Task<AdminRiasecDto> BuildRiasecAsync(
        TestResult result,
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        CancellationToken cancellationToken)
    {
        var normalized = TestResultJson.DeserializeScores(result.NormalizedScoresJson);
        var levels = TestResultJson.DeserializeLevels(result.LevelsJson);

        var types = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var (scaleCode, letter) in RiasecScaleToLetter)
        {
            if (normalized.TryGetValue(scaleCode, out var pct))
            {
                types[letter] = pct;
            }
        }

        var differentiation = normalized.GetValueOrDefault("DIFFERENTIATION");
        var consistency = levels.GetValueOrDefault("CONSISTENCY") ?? string.Empty;

        var careerFields = await ResolveCareerFieldsAsync(result.ResultCode, context, executor, cancellationToken).ConfigureAwait(false);

        return new AdminRiasecDto(result.ResultCode ?? string.Empty, types, differentiation, consistency, careerFields);
    }

    private static AdminActivityDto BuildActivity(TestResult result)
    {
        var normalized = TestResultJson.DeserializeScores(result.NormalizedScoresJson);
        var levels = TestResultJson.DeserializeLevels(result.LevelsJson);
        var flags = TestResultJson.DeserializeFlags(result.FlagsJson);

        var scales = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var scale in new[] { "MOT", "SELF", "SOCA", "ENG" })
        {
            if (normalized.TryGetValue(scale, out var pct))
            {
                scales[scale] = pct;
            }
        }

        var activityLevel = levels.GetValueOrDefault("ACTIVITY");

        return new AdminActivityDto(scales, result.CompositeIndex, string.IsNullOrEmpty(activityLevel) ? null : activityLevel, flags.Contains("NeedsAttention"));
    }

    /// <summary>
    /// `docs/03` §4.3 moslash qoidasi — `GetStudentResultQueryHandler.ResolveCareerFieldsAsync`
    /// bilan bir xil mantiq, lekin admin javobi kasb nomlari (`professions`) bilan ham keladi
    /// (`docs/07` 3.2 misoli).
    /// </summary>
    private static async Task<IReadOnlyList<AdminCareerFieldDto>> ResolveCareerFieldsAsync(
        string? hollandCode,
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(hollandCode))
        {
            return [];
        }

        var codeLetters = hollandCode.ToCharArray();

        var careerMapEntries = await executor.ToListAsync(
            context.AsNoTracking(context.CareerMap).OrderBy(c => c.RelevanceOrder),
            cancellationToken).ConfigureAwait(false);

        return careerMapEntries
            .Where(c => c.HollandCode.All(letter => codeLetters.Contains(letter)))
            .OrderBy(c => c.RelevanceOrder)
            .Select(c => new AdminCareerFieldDto(c.FieldNameUz, c.ExampleProfessions))
            .DistinctBy(c => c.Name)
            .Take(MaxCareerFields)
            .ToList();
    }

    /// <summary>
    /// `docs/07` 3.2 `aiAnalysis`. Mazmun bo'limlari `AiAnalysisContent` orqali yig'iladi —
    /// haqiqat manbai `AiAnalysis.ResponseJson` (`docs/09` 5-bo'lim sxemasi), entity ustunlari
    /// esa faqat zaxira (shablon hisobot va eski yozuvlar) uchun. Shu bilan `learningStyle`,
    /// `motivationProfile`, `activityAssessment`, `disclaimer`, `reliabilityNote` va
    /// `strengths`/`growthAreas`/`attentionFlags` tuzilmasi admin ekraniga yetib boradi.
    /// </summary>
    public static AdminAiAnalysisDto? BuildAiAnalysis(AiAnalysis? current)
    {
        if (current is null)
        {
            return null;
        }

        var content = AiAnalysisContent.From(current);

        return new AdminAiAnalysisDto(
            current.Id,
            current.Status.ToString(),
            current.Provider.ToString(),
            current.Model,
            current.PromptVersion,
            current.CreatedAt,
            current.IsFallbackReport,
            content.IsModerated,
            current.ErrorMessage,
            content.Summary,
            content.PersonalityPortrait,
            content.Strengths,
            content.GrowthAreas,
            content.LearningStyle,
            content.MotivationProfile,
            content.ActivityAssessment,
            content.CareerSuggestions,
            content.StudentRecommendations,
            content.TeacherNotes,
            content.ParentNotes,
            content.AttentionFlags,
            content.ReliabilityNote,
            content.Disclaimer);
    }

    public static IReadOnlyList<AdminAiHistoryItemDto> BuildAiHistory(IReadOnlyList<AiAnalysis> analyses) =>
        analyses
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AdminAiHistoryItemDto(a.Id, a.Provider.ToString(), a.CreatedAt, a.Status.ToString(), a.IsCurrent))
            .ToList();
}
