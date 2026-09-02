using System.Text.Json;
using System.Text.Json.Nodes;

namespace StudentRoadMap.Infrastructure.Ai.Providers;

/// <summary>
/// `AnalysisJsonSchema` (to'liq JSON Schema, `docs/09` 5-bo'lim) ni Gemini `generationConfig.
/// responseSchema` kutgan cheklangan OpenAPI 3.0 qism-to'plamiga moslaydi. Google hujjatlariga
/// ko'ra (2025-holat) `responseSchema` `additionalProperties` kalitini rasman qo'llab-
/// quvvatlamaydi — shu kalit olib tashlanadi, qolgani (`type`, `properties`, `required`,
/// `items`, `enum`, `minLength`/`maxLength`/`minItems`/`maxItems` va h.k.) o'zgarishsiz
/// o'tkaziladi. **Haqiqiy kalit bilan sinalmagan** — `prompts/17` MUHIM eslatma.
/// </summary>
internal static class GeminiSchemaAdapter
{
    private static readonly HashSet<string> UnsupportedKeys = new(StringComparer.Ordinal) { "additionalProperties" };

    public static JsonNode? Convert(JsonElement schema) => Strip(schema);

    private static JsonNode? Strip(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new JsonObject();
                foreach (var property in element.EnumerateObject())
                {
                    if (UnsupportedKeys.Contains(property.Name))
                    {
                        continue;
                    }

                    obj[property.Name] = Strip(property.Value);
                }

                return obj;

            case JsonValueKind.Array:
                var array = new JsonArray();
                foreach (var item in element.EnumerateArray())
                {
                    array.Add(Strip(item));
                }

                return array;

            case JsonValueKind.String:
                return JsonValue.Create(element.GetString());

            case JsonValueKind.Number:
                return JsonValue.Create(element.GetDouble());

            case JsonValueKind.True:
                return JsonValue.Create(true);

            case JsonValueKind.False:
                return JsonValue.Create(false);

            default:
                return null;
        }
    }
}
