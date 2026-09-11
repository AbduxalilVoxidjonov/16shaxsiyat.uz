using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// `RegistrationFields` bilan `assessment_programs.registration_fields` (`jsonb`) ustuni
/// orasidagi (de)serializatsiya — `VisibilityRuleJson` naqshiga o'xshab: maydon nomlari
/// camelCase, enum qiymatlari satr sifatida. `NULL` — "standart qiymatlar ishlatilsin"
/// degani (<see cref="AssessmentProgram.ResolveRegistrationFields"/>).
/// </summary>
public static class RegistrationFieldsJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string? Serialize(RegistrationFields? fields) =>
        fields is null ? null : JsonSerializer.Serialize(fields, Options);

    public static RegistrationFields? Deserialize(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<RegistrationFields>(json, Options);
}
