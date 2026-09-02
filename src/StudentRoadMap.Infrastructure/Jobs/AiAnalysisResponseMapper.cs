using System.Text.Json;

namespace StudentRoadMap.Infrastructure.Jobs;

/// <summary>
/// Validatsiyadan o'tgan AI javobi (`docs/09-ai-analiz-moduli.md` 5-bo'lim JSON sxemasi)ni
/// `AiAnalysis.Succeed(...)` kutayotgan maydonlarga o'giradi.
/// <para>
/// ⚠️ **Muhim moslik eslatmasi** (P18 hisobotida PM'ga alohida qayd etilgan): `docs/09` §5
/// sxemasida `strengths`/`growthAreas`/`attentionFlags` — OBYEKTLAR massivi va
/// `teacherNotes`/`parentNotes` — STRING massivi. Lekin `AiAnalysis.TeacherNotes`/`ParentNotes`
/// ustunlari `text` (bitta satr) va admin javobini yig'uvchi mavjud kod
/// (`Application.Admin.Students.StudentProfileMapping.BuildAiAnalysis`, boshqa agent hududi,
/// P14/P15 da yozilgan) `StrengthsJson`/`GrowthAreasJson`/`AttentionFlagsJson`ni
/// `IReadOnlyList&lt;string&gt;` (`DeserializeStringList`) deb kutadi va `TeacherNotes`/`ParentNotes`ni
/// TO'G'RIDAN-TO'G'RI satr sifatida oladi (massiv deb parse QILMAYDI). Bu klass shu MAVJUD
/// shartnomaga mos keladi — sxemaning obyekt/massiv maydonlarini o'qishga tushunarli satrlarga
/// yassilaydi. To'liq (bironta yo'qotishsiz) ma'lumot baribir `AiAnalysis.ResponseJson`da xom
/// holda saqlanadi.
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

    /// <summary>`Application.Admin.Students.AdminCareerSuggestionDto(Field, Why, NextSteps)` bilan mos — schema maydonlari ustidan to'g'ridan-to'g'ri o'tkaziladi.</summary>
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
