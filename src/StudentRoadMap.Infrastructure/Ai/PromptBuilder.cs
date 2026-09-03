using System.Text.Json;
using System.Text.Json.Serialization;
using StudentRoadMap.Application.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Scoring;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Infrastructure.Ai;

/// <summary>`PromptBuilder.BuildAsync` natijasi — `AiAnalysis` yozuvini to'ldirish uchun kerakli hamma narsa.</summary>
/// <param name="SystemText">Ishlatilgan shablonning tizim matni.</param>
/// <param name="UserText">Foydalanuvchi matni — `{ANALYSIS_INPUT_JSON}` allaqachon almashtirilgan.</param>
/// <param name="PromptVersion">`AiAnalysis.PromptVersion`ga yoziladigan versiya (masalan `v1.0`).</param>
/// <param name="AnalysisInputJson">Faqat strukturalangan kirish (`AiAnalysis.RequestPayloadJson`) — prompt matni EMAS (`docs/04` 2.8-bo'lim izohi).</param>
public sealed record PromptBuildResult(
    string SystemText,
    string UserText,
    string PromptVersion,
    string AnalysisInputJson);

/// <summary>
/// `Assessment` + `TestResult`lardan (Domain qatlamidagi hisoblangan ballardan) `AnalysisInput`
/// JSON'ini quradi va `prompt_templates`dan (yoki bo'sh bo'lsa embedded default'dan) tizim/foydalanuvchi
/// matnlarini oladi (`docs/09-ai-analiz-moduli.md` 3 va 4-bo'lim, `prompts/16` vazifa #2).
/// </summary>
public interface IPromptBuilder
{
    Task<PromptBuildResult> BuildAsync(
        Student student,
        Assessment assessment,
        IReadOnlyList<TestResult> testResults,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IPromptBuilder"/>
public sealed class PromptBuilder : IPromptBuilder
{
    /// <summary>`prompt_templates.key` — hozircha yagona shablon (`docs/09` 4-bo'lim).</summary>
    public const string DefaultTemplateKey = DefaultPromptTemplates.Key;

    private static readonly string[] Mbti16Axes = ["EI", "SN", "TF", "JP"];

    /// <summary>RIASEC bitta harfli mnemonika (`docs/03` §4.1) → bazadagi `scale` kodi — `RiasecStrategy.TypeOrder`/`TypeLetters` bilan bir xil, `StudentProfileMapping`dagi xuddi shu mosliknikiga o'xshash (Application ichki turi ko'rinmasligi sabab bu yerda mustaqil takrorlangan).</summary>
    private static readonly IReadOnlyDictionary<string, string> RiasecScaleToLetter = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["R"] = "R",
        ["I"] = "I",
        ["ART"] = "A",
        ["SOC"] = "S",
        ["ENT"] = "E",
        ["CONV"] = "C",
    };

    private const int MaxMappedCareerFields = 5;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public PromptBuilder(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<PromptBuildResult> BuildAsync(
        Student student,
        Assessment assessment,
        IReadOnlyList<TestResult> testResults,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(student);
        ArgumentNullException.ThrowIfNull(assessment);
        ArgumentNullException.ThrowIfNull(testResults);

        var analysisInput = await BuildAnalysisInputAsync(student, assessment, testResults, now, cancellationToken).ConfigureAwait(false);
        var analysisInputJson = JsonSerializer.Serialize(analysisInput, SerializerOptions);

        var (systemText, userTextTemplate, promptVersion) = await ResolveTemplateAsync(cancellationToken).ConfigureAwait(false);
        var userText = userTextTemplate.Replace(DefaultPromptTemplates.AnalysisInputPlaceholder, analysisInputJson, StringComparison.Ordinal);

        return new PromptBuildResult(systemText, userText, promptVersion, analysisInputJson);
    }

    private async Task<(string SystemText, string UserText, string PromptVersion)> ResolveTemplateAsync(CancellationToken cancellationToken)
    {
        var template = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.PromptTemplates).Where(t => t.Key == DefaultTemplateKey && t.IsActive),
            cancellationToken).ConfigureAwait(false);

        if (template is not null)
        {
            return (template.SystemText, template.UserText, template.Version);
        }

        // `prompt_templates` bo'sh (masalan `--seed` hali ishga tushmagan) — embedded default
        // (`prompts/16`: "jadval bo'sh bo'lsa embedded default v1.0").
        return (DefaultPromptTemplates.SystemTextV1, DefaultPromptTemplates.UserTextV1, DefaultPromptTemplates.Version);
    }

    private async Task<AnalysisInput> BuildAnalysisInputAsync(
        Student student,
        Assessment assessment,
        IReadOnlyList<TestResult> testResults,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var context = new AnalysisContext(
            Age: CalculateAge(student.BirthDate, now),
            Grade: student.Grade,
            Gender: MapGender(student.Gender),
            Language: assessment.LanguageCode);

        var reliability = new AnalysisReliability(
            Score: assessment.ReliabilityScore ?? 0,
            Flag: (assessment.ReliabilityFlag ?? ReliabilityFlag.Reliable).ToString(),
            Notes: []);

        // ⚠️ ENG MUHIM: qaysi natija shaxsiyat tipi/omillar/kasb qiziqishi/aktivlik ekani
        // metodika KODI bilan aniqlanmaydi (`docs/06` 8-bo'lim, 2026-09-02 "dastur" qarori).
        // Ilgari bu yerda `TestCode == "MBTI16"` (va `SystemTestCodes` ro'yxati) turardi:
        // `MBTI16` KODLI `Custom` anketa AI promptiga `personality16` bloki sifatida tushib
        // ketardi (`PERS-BAT-1` kabi boshqa kodli HAQIQIY batareya esa `customTests`ga) — ya'ni
        // o'quvchi jimgina NOTO'G'RI tahlil olardi. Mezon — `PersonalityBattery.RoleOf`
        // (`ScoringStrategyCode` bo'yicha), `CompleteSessionCommandHandler` bilan BIR XIL.
        var rolesByAssessmentTestId = await PersonalityBatteryRoles
            .LoadByAssessmentTestIdAsync(_context, _executor, assessment.Id, cancellationToken)
            .ConfigureAwait(false);

        var mbtiResult = PersonalityBatteryRoles.FindByRole(testResults, rolesByAssessmentTestId, PersonalityBatteryRole.PersonalityType);
        var bigFiveResult = PersonalityBatteryRoles.FindByRole(testResults, rolesByAssessmentTestId, PersonalityBatteryRole.Traits);
        var riasecResult = PersonalityBatteryRoles.FindByRole(testResults, rolesByAssessmentTestId, PersonalityBatteryRole.CareerInterest);
        var activityResult = PersonalityBatteryRoles.FindByRole(testResults, rolesByAssessmentTestId, PersonalityBatteryRole.Activity);

        // Batareya bloklari (`personality16`/`bigFive`/`interests`/`activity`) `null` bo'lsa
        // JSON'ga UMUMAN chiqmaydi (`SerializerOptions.DefaultIgnoreCondition =
        // WhenWritingNull`) — dasturda batareya bo'lmaganda AI'ga `0` yoki bo'sh blok emas,
        // HECH NARSA ketadi: `0` va "ma'lumot yo'q" bir xil emas (`docs/06` 8-bo'lim).
        var personality16 = await BuildPersonality16Async(mbtiResult, cancellationToken).ConfigureAwait(false);
        var bigFive = BuildBigFive(bigFiveResult);
        var interests = await BuildInterestsAsync(riasecResult, cancellationToken).ConfigureAwait(false);
        var activity = BuildActivity(activityResult);
        var customTests = await BuildCustomTestsAsync(testResults, rolesByAssessmentTestId, cancellationToken).ConfigureAwait(false);

        return new AnalysisInput(context, reliability, personality16, bigFive, interests, activity, customTests);
    }

    private async Task<AnalysisPersonality16?> BuildPersonality16Async(TestResult? result, CancellationToken cancellationToken)
    {
        if (result is null)
        {
            return null;
        }

        var normalized = DeserializeScores(result.NormalizedScoresJson);
        var levels = DeserializeLevels(result.LevelsJson);
        var flags = DeserializeFlags(result.FlagsJson);

        var axes = new Dictionary<string, AnalysisAxis>(StringComparer.Ordinal);
        var borderlineAxes = new List<string>();
        foreach (var axis in Mbti16Axes)
        {
            if (!normalized.TryGetValue(axis, out var pct) || !levels.TryGetValue(axis, out var letter))
            {
                continue;
            }

            axes[axis] = new AnalysisAxis(pct, letter);
            if (flags.Contains($"Borderline:{axis}"))
            {
                borderlineAxes.Add(axis);
            }
        }

        var typeNameUz = string.Empty;
        if (!string.IsNullOrEmpty(result.ResultCode))
        {
            var typeCatalogEntry = await _executor.FirstOrDefaultAsync(
                _context.AsNoTracking(_context.TypeCatalog).Where(t => t.Code == result.ResultCode),
                cancellationToken).ConfigureAwait(false);
            typeNameUz = typeCatalogEntry?.NameUz ?? string.Empty;
        }

        return new AnalysisPersonality16(result.ResultCode ?? string.Empty, typeNameUz, axes, borderlineAxes);
    }

    private static AnalysisBigFive? BuildBigFive(TestResult? result)
    {
        if (result is null)
        {
            return null;
        }

        var normalized = DeserializeScores(result.NormalizedScoresJson);
        var levels = DeserializeLevels(result.LevelsJson);

        AnalysisFactorLevel Factor(string code) =>
            new(normalized.GetValueOrDefault(code), levels.GetValueOrDefault(code) ?? string.Empty);

        var stabilityPct = normalized.GetValueOrDefault("STABILITY");

        // `TestResult` da alohida "STABILITY" darajasi saqlanmaydi (`BigFiveStrategy` faqat
        // foizini yozadi) — `docs/09` §3 namunasida esa `stability.level` bor, shu sabab boshqa
        // O/C/E/A omillari bilan bir xil chegaralar (`ScoringConstants`) qo'llaniladi.
        var stabilityLevel = ClassifyBig5Level(stabilityPct);

        return new AnalysisBigFive(
            O: Factor("O"),
            C: Factor("C"),
            E: Factor("E"),
            A: Factor("A"),
            Stability: new AnalysisFactorLevel(stabilityPct, stabilityLevel),
            MaturityIndex: result.CompositeIndex,
            MaturityLevel: levels.GetValueOrDefault("MATURITY"));
    }

    private static string ClassifyBig5Level(double pct) => pct switch
    {
        <= ScoringConstants.Big5LevelVeryLowMax => ScoringConstants.Big5LevelVeryLow,
        <= ScoringConstants.Big5LevelLowMax => ScoringConstants.Big5LevelLow,
        <= ScoringConstants.Big5LevelAverageMax => ScoringConstants.Big5LevelAverage,
        <= ScoringConstants.Big5LevelHighMax => ScoringConstants.Big5LevelHigh,
        _ => ScoringConstants.Big5LevelVeryHigh,
    };

    private async Task<AnalysisInterests?> BuildInterestsAsync(TestResult? result, CancellationToken cancellationToken)
    {
        if (result is null)
        {
            return null;
        }

        var normalized = DeserializeScores(result.NormalizedScoresJson);
        var levels = DeserializeLevels(result.LevelsJson);

        var types = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var (scaleCode, letter) in RiasecScaleToLetter)
        {
            if (normalized.TryGetValue(scaleCode, out var pct))
            {
                types[letter] = pct;
            }
        }

        var mappedFields = await ResolveMappedFieldsAsync(result.ResultCode, cancellationToken).ConfigureAwait(false);

        return new AnalysisInterests(
            HollandCode: result.ResultCode ?? string.Empty,
            Types: types,
            Differentiation: normalized.GetValueOrDefault("DIFFERENTIATION"),
            Consistency: levels.GetValueOrDefault("CONSISTENCY") ?? string.Empty,
            MappedFields: mappedFields);
    }

    /// <summary>
    /// `docs/03` §4.3 moslash qoidasi — `GetStudentResultQueryHandler.ResolveCareerFieldsAsync`
    /// bilan bir xil mantiq (Application ichki turi ko'rinmagani sabab mustaqil takrorlangan).
    /// </summary>
    private async Task<IReadOnlyList<string>> ResolveMappedFieldsAsync(string? hollandCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(hollandCode))
        {
            return [];
        }

        var codeLetters = hollandCode.ToCharArray();

        var careerMapEntries = await _executor.ToListAsync(
            _context.AsNoTracking(_context.CareerMap).OrderBy(c => c.RelevanceOrder),
            cancellationToken).ConfigureAwait(false);

        return careerMapEntries
            .Where(c => c.HollandCode.All(letter => codeLetters.Contains(letter)))
            .OrderBy(c => c.RelevanceOrder)
            .Select(c => c.FieldNameUz)
            .Distinct()
            .Take(MaxMappedCareerFields)
            .ToList();
    }

    private static AnalysisActivity? BuildActivity(TestResult? result)
    {
        if (result is null)
        {
            return null;
        }

        var normalized = DeserializeScores(result.NormalizedScoresJson);
        var levels = DeserializeLevels(result.LevelsJson);
        var flags = DeserializeFlags(result.FlagsJson);

        return new AnalysisActivity(
            Mot: normalized.GetValueOrDefault("MOT"),
            Self: normalized.GetValueOrDefault("SELF"),
            Soca: normalized.GetValueOrDefault("SOCA"),
            Eng: normalized.GetValueOrDefault("ENG"),
            ActivityIndex: result.CompositeIndex,
            ActivityLevel: levels.GetValueOrDefault("ACTIVITY"),
            NeedsAttention: flags.Contains("NeedsAttention"));
    }

    /// <summary>
    /// `Custom` — batareya ROLI yo'q (`PersonalityBatteryRole.None`) har qanday natija: superadmin
    /// anketalari (`SUM`) va batareyaga kirmaydigan boshqa bloklar. Ilgari bu ro'yxat qat'iy
    /// `SystemTestCodes` (`"MBTI16"`, `"BIG5"`, ...) satrlari bo'yicha ajratilardi — natijada
    /// `MBTI16` KODLI superadmin anketasi `customTests`dan TUSHIB QOLARDI (va yuqorida
    /// `personality16` sifatida noto'g'ri talqin qilinardi).
    /// </summary>
    private async Task<IReadOnlyList<AnalysisCustomTest>> BuildCustomTestsAsync(
        IReadOnlyList<TestResult> testResults,
        IReadOnlyDictionary<Guid, PersonalityBatteryRole> rolesByAssessmentTestId,
        CancellationToken cancellationToken)
    {
        var customResults = testResults
            .Where(r => !rolesByAssessmentTestId.ContainsKey(r.AssessmentTestId))
            .ToList();
        if (customResults.Count == 0)
        {
            return [];
        }

        var codes = customResults.Select(r => r.TestCode).Distinct().ToList();
        var definitions = await _executor.ToListAsync(
            _context.AsNoTracking(_context.TestDefinitions).Where(d => codes.Contains(d.Code)),
            cancellationToken).ConfigureAwait(false);
        var namesByCode = definitions.ToDictionary(d => d.Code, d => d.NameUz, StringComparer.Ordinal);

        var customTests = new List<AnalysisCustomTest>();
        foreach (var result in customResults)
        {
            var normalized = DeserializeScores(result.NormalizedScoresJson);
            var levels = DeserializeLevels(result.LevelsJson);

            // Domen modelida shkala uchun alohida "insonga tushunarli nom" saqlanmaydi
            // (`Question.Scale` faqat kod) — shu sabab shkala KODI nom sifatida ishlatiladi.
            var scales = normalized
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => new AnalysisCustomTestScale(kv.Key, kv.Value, levels.GetValueOrDefault(kv.Key) ?? string.Empty))
                .ToList();

            var name = namesByCode.GetValueOrDefault(result.TestCode, result.TestCode);
            customTests.Add(new AnalysisCustomTest(name, scales));
        }

        return customTests;
    }

    private static int CalculateAge(DateOnly birthDate, DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    private static string MapGender(Gender gender) => gender switch
    {
        Gender.Male => "male",
        Gender.Female => "female",
        _ => "unspecified",
    };

    private static IReadOnlyDictionary<string, double> DeserializeScores(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, double>>(json) ?? [];

    private static IReadOnlyDictionary<string, string> DeserializeLevels(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];

    private static IReadOnlyList<string> DeserializeFlags(string json) =>
        JsonSerializer.Deserialize<List<string>>(json) ?? [];
}
