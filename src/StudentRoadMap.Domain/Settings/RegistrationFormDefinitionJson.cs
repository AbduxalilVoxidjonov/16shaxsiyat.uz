using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudentRoadMap.Domain.Settings;

/// <summary>
/// `RegistrationFormDefinition` bilan `registration_form_settings.definition` (`jsonb`) ustuni
/// orasidagi (de)serializatsiya — `RegistrationFieldsJson`/`VisibilityRuleJson` bilan bir xil
/// naqsh: maydon nomlari camelCase, enum qiymatlari satr sifatida.
/// </summary>
public static class RegistrationFormDefinitionJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(RegistrationFormDefinition definition) =>
        JsonSerializer.Serialize(definition, Options);

    /// <summary>`null`/bo'sh — "sozlama hali saqlanmagan" (chaqiruvchida `RegistrationFormDefinition.Default` ishlatiladi).</summary>
    public static RegistrationFormDefinition? Deserialize(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<RegistrationFormDefinition>(json, Options);
}
