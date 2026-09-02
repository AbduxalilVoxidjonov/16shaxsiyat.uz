using System.Text.Json;
using System.Text.Json.Nodes;

namespace StudentRoadMap.Infrastructure.Ai.Providers;

/// <summary>
/// `AnalysisJsonSchema` (kanonik JSON Schema, `docs/09` 5-bo'lim — validatorning haqiqat
/// manbai, uchala provider uchun umumiy, **o'zgartirilmaydi**) ni OpenAI `response_format:
/// json_schema, strict: true` talab qiladigan qat'iy ko'rinishga moslaydi: har obyektning
/// BARCHA `properties` kaliti `required`ga qo'shiladi, ilgari ixtiyoriy bo'lganlarining
/// `type`iga `"null"` qo'shiladi (masalan `"type": ["array","null"]`). Kanonik sxemadagi
/// "haqiqiy" (majburiy/ixtiyoriy) farq yo'qolmaydi — faqat OpenAI'ga jo'natishda vaqtincha
/// "hammasi required, ixtiyoriylari nullable" ko'rinishiga o'giriladi; javob esa
/// `OpenAiResponseNormalizer` bilan orqaga qaytariladi. PM ko'rsatmasi (`prompts/17` javob
/// xati, 2026-09-02): "sxema bitta vendor talabiga egilmaydi — provider tomonda moslashtir".
/// </summary>
internal static class OpenAiSchemaAdapter
{
    public static JsonNode ToStrictSchema(JsonElement schema)
    {
        var node = JsonNode.Parse(schema.GetRawText())
            ?? throw new InvalidOperationException("Kanonik sxema bo'sh — 'null' hujjat kutilmagan.");
        Transform(node);
        return node;
    }

    private static void Transform(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                TransformObjectSchema(obj);
                break;
            case JsonArray array:
                foreach (var item in array)
                {
                    Transform(item);
                }

                break;
        }
    }

    private static void TransformObjectSchema(JsonObject schemaObj)
    {
        // Ichma-ich sxemalar ham bo'lishi mumkin bo'lgan joylar: "properties" ichidagi har bir
        // qiymat va "items" (massiv elementi sxemasi).
        if (schemaObj.TryGetPropertyValue("items", out var itemsNode))
        {
            Transform(itemsNode);
        }

        if (schemaObj.TryGetPropertyValue("properties", out var propertiesNode) && propertiesNode is JsonObject properties)
        {
            var propertyKeys = properties.Select(p => p.Key).ToList();
            foreach (var key in propertyKeys)
            {
                Transform(properties[key]);
            }

            var originalRequired = schemaObj.TryGetPropertyValue("required", out var requiredNode) && requiredNode is JsonArray requiredArray
                ? requiredArray.Select(r => r!.GetValue<string>()).ToHashSet(StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);

            foreach (var key in propertyKeys)
            {
                if (!originalRequired.Contains(key))
                {
                    MakeNullable(properties[key]);
                }
            }

            // OpenAI `strict` talabi: HAR bir `properties` kaliti `required`da bo'lishi shart —
            // ixtiyoriylik endi `type`dagi `"null"` orqali ifodalanadi (yuqorida).
            schemaObj["required"] = new JsonArray(propertyKeys.Select(k => (JsonNode)JsonValue.Create(k)!).ToArray());
        }
    }

    private static void MakeNullable(JsonNode? propertySchema)
    {
        if (propertySchema is not JsonObject schemaObj || !schemaObj.TryGetPropertyValue("type", out var typeNode) || typeNode is null)
        {
            // "type" yo'q (masalan faqat "$ref" yoki "enum") — hozirgi kanonik sxemada
            // uchramaydi, himoyaviy holat sifatida o'zgartirilmay qoldiriladi.
            return;
        }

        if (typeNode is JsonArray typeArray)
        {
            if (!typeArray.Any(t => t?.GetValue<string>() == "null"))
            {
                typeArray.Add(JsonValue.Create("null"));
            }

            return;
        }

        var currentType = typeNode.GetValue<string>();
        if (currentType == "null")
        {
            return;
        }

        schemaObj["type"] = new JsonArray(JsonValue.Create(currentType), JsonValue.Create("null"));
    }
}
