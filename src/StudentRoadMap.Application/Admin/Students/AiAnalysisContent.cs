using System.Text.Json;
using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Application.Admin.Students;

/// <summary>
/// `AiAnalysis` yozuvidan admin hisobotining MAZMUN bo'limlarini yig'adi.
///
/// **Haqiqat manbai — `docs/09-ai-analiz-moduli.md` 5-bo'lim JSON sxemasi**: AI aynan shunga
/// javob beradi, `AiResponseValidator` (`docs/09` 6-bo'lim) shu sxema bo'yicha tekshiradi va
/// faqat tekshiruvdan o'tgan javob `AiAnalysis.ResponseJson`ga yoziladi. Shu sabab mazmun
/// BIRINCHI NAVBATDA `ResponseJson`dan o'qiladi.
///
/// <para>
/// Entity ustunlari (`StrengthsJson`, `TeacherNotes`, …) — o'sha javobning YASSILANGAN
/// (satrga aylantirilgan) nusxasi (`Infrastructure/Jobs/AiAnalysisResponseMapper`): ular
/// tuzilmani (`description`/`evidence`/`severity`) va sxemadagi beshta maydonni
/// (`learningStyle`, `motivationProfile`, `activityAssessment`, `disclaimer`,
/// `reliabilityNote`) saqlamaydi. Shuning uchun ustunlar faqat ZAXIRA manba: har bir bo'lim
/// uchun avval `ResponseJson` o'qiladi, u yo'q/bo'sh bo'lsagina ustunga tushiladi. Bu,
/// birinchi navbatda, shablon (fallback) hisobot (`docs/09` 11-bo'lim,
/// `AiAnalysis.CreateFallbackReport` `ResponseJson` YOZMAYDI) va AI moduli ishga
/// tushishidan oldin yaratilgan eski yozuvlar uchun kerak.
/// </para>
///
/// <para>
/// **Post-filtr (`CLAUDE.md` 6-qoida).** `ResponseJson` — validatsiyadan o'tgan javob, lekin
/// `docs/09` 6-bo'lim 3-bandiga ko'ra taqiqlangan atama IKKINCHI urinishda ham topilsa javob
/// baribir saqlanadi va `AttentionFlags` ustuniga `MODERATION_REQUIRED` qo'shiladi (bu belgi
/// FAQAT ustunda bo'ladi, `ResponseJson`da emas). Shu sabab bayroqlar ikkala manbadan
/// BIRLASHTIRILADI va moderatsiya belgisi <see cref="IsModerated"/> orqali DTO'ga ochiq
/// chiqariladi — moderatsiya qilingan xom matn ekranga JIMGINA (belgisiz) chiqib ketmaydi.
/// </para>
/// </summary>
internal sealed record AiAnalysisContent(
    string? Summary,
    string? PersonalityPortrait,
    IReadOnlyList<AdminAiStrengthDto> Strengths,
    IReadOnlyList<AdminAiGrowthAreaDto> GrowthAreas,
    string? LearningStyle,
    string? MotivationProfile,
    string? ActivityAssessment,
    IReadOnlyList<AdminCareerSuggestionDto> CareerSuggestions,
    IReadOnlyList<string> StudentRecommendations,
    IReadOnlyList<string> TeacherNotes,
    IReadOnlyList<string> ParentNotes,
    IReadOnlyList<AdminAiAttentionFlagDto> AttentionFlags,
    string? ReliabilityNote,
    string? Disclaimer,
    bool IsModerated)
{
    /// <summary>`docs/09` 6-bo'lim, 3-band — moderatsiya belgisi kodi.</summary>
    private const string ModerationFlagCode = "MODERATION_REQUIRED";

    /// <summary>`docs/09` 5-bo'lim: `severity` enum qiymatlari.</summary>
    private static readonly string[] AllowedSeverities = ["info", "attention", "high"];

    private const string DefaultSeverity = "attention";

    public static AiAnalysisContent From(AiAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        using var response = TryParseObject(analysis.ResponseJson);
        var root = response?.RootElement;

        var (moderationFlags, legacyFlags) = SplitColumnFlags(DeserializeStringList(analysis.AttentionFlagsJson));

        // Har bir bo'lim uchun: AVVAL sxema javobi (`ResponseJson`), u yo'q/bo'sh bo'lsa —
        // ustundagi yassilangan nusxa. Shu bilan eski (sxemasiz) yozuvlar ham, shablon hisobot
        // ham ekranda bo'sh qolmaydi.
        var strengths = FirstNonEmpty(
            ReadObjectArray(root, "strengths", el => new AdminAiStrengthDto(
                ReadString(el, "title") ?? string.Empty,
                ReadString(el, "description"),
                ReadString(el, "evidence"))),
            () => DeserializeStringList(analysis.StrengthsJson)
                .Select(text => new AdminAiStrengthDto(text, null, null))
                .ToList());

        var growthAreas = FirstNonEmpty(
            ReadObjectArray(root, "growthAreas", el => new AdminAiGrowthAreaDto(
                ReadString(el, "title") ?? string.Empty,
                ReadString(el, "description"),
                ReadString(el, "actionStep"))),
            () => DeserializeStringList(analysis.GrowthAreasJson)
                .Select(text => new AdminAiGrowthAreaDto(text, null, null))
                .ToList());

        var careerSuggestions = FirstNonEmpty(
            ReadObjectArray(root, "careerSuggestions", el => new AdminCareerSuggestionDto(
                ReadString(el, "field") ?? string.Empty,
                ReadString(el, "why") ?? string.Empty,
                ReadStringArray(el, "exampleProfessions"),
                ReadStringArray(el, "nextSteps"))),
            () => DeserializeCareerSuggestions(analysis.CareerSuggestionsJson));

        // Moderatsiya bayrog'i FAQAT ustunda bo'ladi (`AnalysisOrchestrator.AppendModerationFlag`) —
        // sxemadagi bayroqlar ustiga QO'SHILADI, aks holda "moderatsiya qilindi" belgisi yo'qolardi.
        var attentionFlags = FirstNonEmpty(
            ReadObjectArray(root, "attentionFlags", ReadAttentionFlag),
            () => legacyFlags);

        return new AiAnalysisContent(
            Summary: NullIfBlank(analysis.Summary) ?? ReadString(root, "summary"),
            PersonalityPortrait: NullIfBlank(analysis.PersonalityPortrait) ?? ReadString(root, "personalityPortrait"),
            Strengths: strengths,
            GrowthAreas: growthAreas,
            LearningStyle: ReadString(root, "learningStyle"),
            MotivationProfile: ReadString(root, "motivationProfile"),
            ActivityAssessment: ReadString(root, "activityAssessment"),
            CareerSuggestions: careerSuggestions,
            StudentRecommendations: FirstNonEmpty(
                ReadStringArray(root, "studentRecommendations"),
                () => DeserializeStringList(analysis.RecommendationsJson)),
            TeacherNotes: FirstNonEmpty(
                ReadStringArray(root, "teacherNotes"),
                () => SplitNotes(analysis.TeacherNotes)),
            ParentNotes: FirstNonEmpty(
                ReadStringArray(root, "parentNotes"),
                () => SplitNotes(analysis.ParentNotes)),
            AttentionFlags: [.. attentionFlags, .. moderationFlags],
            ReliabilityNote: ReadString(root, "reliabilityNote"),
            Disclaimer: ReadString(root, "disclaimer"),
            IsModerated: moderationFlags.Count > 0);
    }

    private static IReadOnlyList<T> FirstNonEmpty<T>(IReadOnlyList<T> preferred, Func<IReadOnlyList<T>> fallback) =>
        preferred.Count > 0 ? preferred : fallback();

    // --- JSON o'qish yordamchilari --------------------------------------------------------

    private static JsonDocument? TryParseObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                document.Dispose();
                return null;
            }

            return document;
        }
        catch (JsonException)
        {
            // Buzuq JSON butun profilni yiqitmasin — ustunlardagi zaxira nusxaga qaytiladi.
            return null;
        }
    }

    private static string? ReadString(JsonElement? element, string propertyName) =>
        element is null ? null : ReadString(element.Value, propertyName);

    private static IReadOnlyList<string> ReadStringArray(JsonElement? element, string propertyName) =>
        element is null ? [] : ReadStringArray(element.Value, propertyName);

    private static IReadOnlyList<T> ReadObjectArray<T>(JsonElement? element, string propertyName, Func<JsonElement, T> project) =>
        element is null ? [] : ReadObjectArray(element.Value, propertyName, project);

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(propertyName, out var value)
        && value.ValueKind == JsonValueKind.String
            ? NullIfBlank(value.GetString())
            : null;

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out var array)
            || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray()
            .Where(el => el.ValueKind == JsonValueKind.String)
            .Select(el => el.GetString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .ToList();
    }

    private static IReadOnlyList<T> ReadObjectArray<T>(JsonElement element, string propertyName, Func<JsonElement, T> project)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out var array)
            || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray()
            .Where(el => el.ValueKind == JsonValueKind.Object)
            .Select(project)
            .ToList();
    }

    private static AdminAiAttentionFlagDto ReadAttentionFlag(JsonElement element) =>
        new(
            ReadString(element, "code"),
            ReadString(element, "message") ?? string.Empty,
            NormalizeSeverity(ReadString(element, "severity")));

    private static string NormalizeSeverity(string? severity) =>
        severity is not null && AllowedSeverities.Contains(severity, StringComparer.OrdinalIgnoreCase)
            ? severity.ToLowerInvariant()
            : DefaultSeverity;

    // --- Ustunlardan (zaxira manba) o'qish -------------------------------------------------

    /// <summary>
    /// Ustundagi satrli bayroqlarni ikkiga ajratadi: moderatsiya belgisi (`MODERATION_REQUIRED`)
    /// va qolgan "eski" bayroqlar (`AiAnalysisResponseMapper` yozgan `"[severity] message"`
    /// shakli yoki shablon hisobotning oddiy jumlasi).
    /// </summary>
    private static (IReadOnlyList<AdminAiAttentionFlagDto> Moderation, IReadOnlyList<AdminAiAttentionFlagDto> Legacy) SplitColumnFlags(
        IReadOnlyList<string> columnFlags)
    {
        var moderation = new List<AdminAiAttentionFlagDto>();
        var legacy = new List<AdminAiAttentionFlagDto>();

        foreach (var raw in columnFlags)
        {
            if (raw.StartsWith(ModerationFlagCode, StringComparison.Ordinal))
            {
                var message = raw[ModerationFlagCode.Length..].TrimStart(':', ' ');
                moderation.Add(new AdminAiAttentionFlagDto(
                    ModerationFlagCode,
                    string.IsNullOrWhiteSpace(message) ? raw : message,
                    "high"));
                continue;
            }

            legacy.Add(ParseLegacyFlag(raw));
        }

        return (moderation, legacy);
    }

    /// <summary>`"[high] matn"` → `severity = "high"`, `message = "matn"`; prefiks bo'lmasa butun satr xabar bo'ladi.</summary>
    private static AdminAiAttentionFlagDto ParseLegacyFlag(string raw)
    {
        if (raw.StartsWith('['))
        {
            var closing = raw.IndexOf(']', StringComparison.Ordinal);
            if (closing > 1)
            {
                var severity = raw[1..closing];
                if (AllowedSeverities.Contains(severity, StringComparer.OrdinalIgnoreCase))
                {
                    return new AdminAiAttentionFlagDto(null, raw[(closing + 1)..].Trim(), severity.ToLowerInvariant());
                }
            }
        }

        return new AdminAiAttentionFlagDto(null, raw, DefaultSeverity);
    }

    /// <summary>
    /// `TeacherNotes`/`ParentNotes` ustuni — bitta `text` (`docs/05` 2-bo'lim), unga
    /// `AiAnalysisResponseMapper` massivni `"• ..."` qatorlari sifatida yozadi. Sxemadagi
    /// massiv shakliga qaytariladi.
    /// </summary>
    private static IReadOnlyList<string> SplitNotes(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.TrimStart('•', '-', ' ').Trim())
            .Where(line => line.Length > 0)
            .ToList();
    }

    private static IReadOnlyList<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return (JsonSerializer.Deserialize<List<string>>(json) ?? [])
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();
        }
        catch (JsonException)
        {
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
            var raw = JsonSerializer.Deserialize<List<CareerSuggestionRaw>>(json, CareerSuggestionOptions) ?? [];
            return raw
                .Select(r => new AdminCareerSuggestionDto(
                    r.Field ?? string.Empty,
                    r.Why ?? string.Empty,
                    r.ExampleProfessions ?? [],
                    r.NextSteps ?? []))
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static readonly JsonSerializerOptions CareerSuggestionOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Ustundagi JSON'da maydon yetishmasligi mumkin — `null` kelib, keyin bo'sh ro'yxatga aylantiriladi.</summary>
    private sealed record CareerSuggestionRaw(
        string? Field,
        string? Why,
        IReadOnlyList<string>? ExampleProfessions,
        IReadOnlyList<string>? NextSteps);

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
