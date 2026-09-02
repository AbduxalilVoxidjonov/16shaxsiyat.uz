using System.Text.Json;
using System.Text.Json.Nodes;

namespace StudentRoadMap.Infrastructure.Ai.Providers;

/// <summary>
/// `OpenAiSchemaAdapter`ning aksi: OpenAI `strict` javobida ixtiyoriy (kanonik sxemada
/// `required`da bo'lmagan) maydonlar model tomonidan to'ldirilmasa `null` sifatida keladi —
/// bu maydonlar kanonik `AnalysisJsonSchema` (`AiResponseValidator` 2-bosqichi) bo'yicha
/// `null`ni QABUL QILMAYDI (faqat `reliabilityNote` kabi allaqachon nullable e'lon qilinganlar
/// bundan mustasno). Shuning uchun javobni validatorga uzatishdan OLDIN — ixtiyoriy va `null`
/// bo'lgan kalitlar butunlay OLIB TASHLANADI (qiymat `null`ga o'zgartirilmaydi, KALIT
/// o'chiriladi), natijada kanonik sxema uni "berilmagan" deb ko'radi. PM ko'rsatmasi
/// (`prompts/17` javob xati, 2026-09-02): "aks holda kanonik sxema bo'yicha null tip xatosi
/// beradi va biz o'zimiz yaratgan muammoga uriladi".
/// </summary>
internal static class OpenAiResponseNormalizer
{
    /// <summary>`responseJson`ni kanonik sxemaga solishtirib normallashtiradi va qayta kompakt JSON matn qilib qaytaradi.</summary>
    public static string Normalize(string responseJson, JsonElement canonicalSchema)
    {
        var node = JsonNode.Parse(responseJson)
            ?? throw new JsonException("AI javobi 'null' sifatida parse qilindi.");
        Strip(node, canonicalSchema);
        return node.ToJsonString();
    }

    private static void Strip(JsonNode? valueNode, JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (valueNode is JsonObject valueObj && schema.TryGetProperty("properties", out var propertiesSchema))
        {
            var requiredKeys = schema.TryGetProperty("required", out var requiredEl)
                ? requiredEl.EnumerateArray().Select(r => r.GetString()!).ToHashSet(StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);

            // `.ToList()` — quyidagi `Remove` bilan bir vaqtda enumeratsiya qilinmasligi uchun nusxa.
            foreach (var (key, value) in valueObj.ToList())
            {
                if (!propertiesSchema.TryGetProperty(key, out var propertySchema))
                {
                    // Kanonik sxemada yo'q kalit — bo'lmasligi kerak (`additionalProperties:false`
                    // ikkala tomonda ham qulflagan), himoyaviy holat sifatida teginilmaydi.
                    continue;
                }

                if (value is null)
                {
                    if (!requiredKeys.Contains(key))
                    {
                        valueObj.Remove(key);
                    }

                    // `required` bo'lgan maydon `null` qaytgan bo'lsa — bu HAQIQIY xato
                    // (model strict shartni buzdi), qasddan olib tashlanmaydi: validator buni
                    // schema-bosqichida tutib, retry so'raydi.
                    continue;
                }

                Strip(value, propertySchema);
            }

            return;
        }

        if (valueNode is JsonArray valueArray && schema.TryGetProperty("items", out var itemSchema))
        {
            foreach (var item in valueArray)
            {
                Strip(item, itemSchema);
            }
        }
    }
}
