using System.Text.Json;

namespace StudentRoadMap.Infrastructure.Jobs;

/// <summary>
/// Validatsiyadan o'tgan AI javobi (`docs/09-ai-analiz-moduli.md` 5-bo'lim JSON sxemasi)ni
/// `AiAnalysis.Succeed(...)` kutayotgan USTUN qiymatlariga o'giradi.
/// <para>
/// **Ustunlar — ikkilamchi nusxa.** `docs/09` §5 sxemasida `strengths`/`growthAreas`/
/// `attentionFlags` — OBYEKTLAR massivi, `teacherNotes`/`parentNotes` — STRING massivi;
/// `AiAnalysis` ustunlari esa (`docs/05` 2-bo'lim DDL) `text`/`jsonb` satr ro'yxati. Bu klass
/// aynan shu ustun shakliga yassilaydi — ro'yxatlarda/hisobotlarda tez o'qish uchun.
/// TO'LIQ (yo'qotishsiz) javob `AiAnalysis.ResponseJson`da xom holda saqlanadi va admin
/// hisoboti (`Application.Admin.Students.AiAnalysisContent`) AYNAN o'shani o'qiydi — shu sabab
/// bu yerdagi yassilanish admin ekranida hech narsani kamaytirmaydi (2026-09-02 moslashtirish;
/// ilgari DTO faqat shu ustunlarni qaytarardi va sxemaning yarmi admindan yashirin qolardi).
/// </para>
/// <para>
/// ⚠️ `AttentionFlagsJson` shakli (`List&lt;string&gt;`) `AnalysisOrchestrator.AppendModerationFlag`
/// bilan bog'liq: moderatsiya belgisi (`docs/09` 6-bo'lim, 3-band) shu ro'yxatga qo'shiladi.
/// Shaklni o'zgartirsangiz o'sha metodni ham yangilang.
/// </para>
/// </summary>
internal static class AiAnalysisResponseMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNamingPolicy = null };

    public sealed record MappedFields(
        string Summary,
        string PersonalityPortrait,
        string StrengthsJson,
        string GrowthAreasJson,
        string CareerSuggestionsJson,
        string RecommendationsJson,
        string? TeacherNotes,
        string? ParentNotes,
        string AttentionFlagsJson);

    public static MappedFields Map(JsonElement root)
    {
        var summary = GetString(root, "summary") ?? string.Empty;
        var personalityPortrait = GetString(root, "personalityPortrait") ?? string.Empty;

        var strengths = MapObjectArray(root, "strengths", el =>
        {
            var title = GetString(el, "title");
            var description = GetString(el, "description");
            var evidence = GetString(el, "evidence");
            return JoinNonEmpty(" — ", title, description) is { Length: > 0 } text
                ? string.IsNullOrWhiteSpace(evidence) ? text : $"{text} ({evidence})"
                : evidence ?? string.Empty;
        });

        var growthAreas = MapObjectArray(root, "growthAreas", el =>
        {
            var title = GetString(el, "title");
            var description = GetString(el, "description");
            var actionStep = GetString(el, "actionStep");
            var text = JoinNonEmpty(" — ", title, description);
            return string.IsNullOrWhiteSpace(actionStep) ? text : $"{text}. Qadam: {actionStep}";
        });

        var careerSuggestions = MapCareerSuggestions(root);

        var studentRecommendations = MapStringArray(root, "studentRecommendations");

        var teacherNotes = JoinBulletList(MapStringArray(root, "teacherNotes"));
        var parentNotes = JoinBulletList(MapStringArray(root, "parentNotes"));

        var attentionFlags = MapObjectArray(root, "attentionFlags", el =>
        {
            var severity = GetString(el, "severity");
            var message = GetString(el, "message") ?? string.Empty;
            return string.IsNullOrWhiteSpace(severity) ? message : $"[{severity}] {message}";
        });

        return new MappedFields(
            summary,
            personalityPortrait,
            JsonSerializer.Serialize(strengths, SerializerOptions),
            JsonSerializer.Serialize(growthAreas, SerializerOptions),
            careerSuggestions,
            JsonSerializer.Serialize(studentRecommendations, SerializerOptions),
            teacherNotes,
            parentNotes,
            JsonSerializer.Serialize(attentionFlags, SerializerOptions));
    }

    /// <summary>`Application.Admin.Students.AdminCareerSuggestionDto(Field, Why, ExampleProfessions, NextSteps)` bilan mos — sxema maydonlari ustidan to'g'ridan-to'g'ri o'tkaziladi.</summary>
    private static string MapCareerSuggestions(JsonElement root)
    {
        if (!root.TryGetProperty("careerSuggestions", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return "[]";
        }

        var items = new List<object>();
        foreach (var el in array.EnumerateArray())
        {
            items.Add(new
            {
                field = GetString(el, "field") ?? string.Empty,
                why = GetString(el, "why") ?? string.Empty,
                // `docs/09` §5da ixtiyoriy maydon — ilgari bu yerda tushib qolar edi.
                exampleProfessions = MapStringArray(el, "exampleProfessions"),
                nextSteps = MapStringArray(el, "nextSteps"),
            });
        }

        return JsonSerializer.Serialize(items, SerializerOptions);
    }

    private static List<string> MapObjectArray(JsonElement root, string propertyName, Func<JsonElement, string> project)
    {
        if (!root.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var results = new List<string>();
        foreach (var el in array.EnumerateArray())
        {
            var text = project(el);
            if (!string.IsNullOrWhiteSpace(text))
            {
                results.Add(text);
            }
        }

        return results;
    }

    private static List<string> MapStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray()
            .Where(el => el.ValueKind == JsonValueKind.String)
            .Select(el => el.GetString() ?? string.Empty)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private static string? JoinBulletList(IReadOnlyList<string> items) =>
        items.Count == 0 ? null : string.Join("\n", items.Select(i => $"• {i}"));

    private static string JoinNonEmpty(string separator, params string?[] parts) =>
        string.Join(separator, parts.Where(p => !string.IsNullOrWhiteSpace(p)));

    private static string? GetString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
