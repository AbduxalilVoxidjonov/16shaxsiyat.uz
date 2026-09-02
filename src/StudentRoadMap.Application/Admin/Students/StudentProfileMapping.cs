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

    public static async Task<AdminTestResultsDto> BuildTestResultsAsync(
        IReadOnlyList<TestResult> testResults,
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        CancellationToken cancellationToken)
    {
        var mbti = testResults.FirstOrDefault(r => r.TestCode == "MBTI16");
        var bigFive = testResults.FirstOrDefault(r => r.TestCode == "BIG5");
        var riasec = testResults.FirstOrDefault(r => r.TestCode == "RIASEC");
        var activity = testResults.FirstOrDefault(r => r.TestCode == "ACTIVITY");

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

    public static AdminAiAnalysisDto? BuildAiAnalysis(AiAnalysis? current)
    {
        if (current is null)
        {
            return null;
        }

        return new AdminAiAnalysisDto(
            current.Id,
            current.Status.ToString(),
            current.Provider.ToString(),
            current.Model,
            current.PromptVersion,
            current.CreatedAt,
            current.Summary,
            current.PersonalityPortrait,
            DeserializeStringList(current.StrengthsJson),
            DeserializeStringList(current.GrowthAreasJson),
            DeserializeCareerSuggestions(current.CareerSuggestionsJson),
            DeserializeStringList(current.RecommendationsJson),
            current.TeacherNotes,
            current.ParentNotes,
            DeserializeStringList(current.AttentionFlagsJson));
    }

    public static IReadOnlyList<AdminAiHistoryItemDto> BuildAiHistory(IReadOnlyList<AiAnalysis> analyses) =>
        analyses
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AdminAiHistoryItemDto(a.Id, a.Provider.ToString(), a.CreatedAt, a.Status.ToString(), a.IsCurrent))
            .ToList();

    private static IReadOnlyList<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (System.Text.Json.JsonException)
        {
            // AI javobi hali P16-18 shakliga to'liq mos kelmasligi mumkin — noto'g'ri JSON
            // butun profilni yiqitmasin, faqat bo'sh ro'yxat qaytadi.
            return [];
        }
    }

    private static IReadOnlyList<AdminCareerSuggestionDto> DeserializeCareerSuggestions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<AdminCareerSuggestionDto>>(json, JsonOptions) ?? [];
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}
