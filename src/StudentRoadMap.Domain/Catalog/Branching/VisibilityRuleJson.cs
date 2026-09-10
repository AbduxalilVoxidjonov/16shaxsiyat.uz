using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudentRoadMap.Domain.Catalog.Branching;

/// <summary>
/// `VisibilityRule` bilan `jsonb` ustunlar (`questions.visibility_rule`,
/// `question_sections.visibility_rule`) orasidagi (de)serializatsiya — `docs/18` §2.4 shakli:
/// maydon nomlari camelCase, enum qiymatlari satr sifatida (`Application.Public.Common.TestResultJson`
/// naqshiga o'xshab, lekin bu yerda `Domain` qatlamida — `System.Text.Json` BCL qismi bo'lgani
/// uchun `docs/06` 3-bo'lim qoidasini (EF/HTTP taqiqlangan) buzmaydi).
/// </summary>
public static class VisibilityRuleJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string? Serialize(VisibilityRule? rule) =>
        rule is null ? null : JsonSerializer.Serialize(rule, Options);

    public static VisibilityRule? Deserialize(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<VisibilityRule>(json, Options);
}
