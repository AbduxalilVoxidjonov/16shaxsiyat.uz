using System.Text.Json;
using StudentRoadMap.Domain.Settings;

namespace StudentRoadMap.Application.Admin.Students;

/// <summary>
/// `Student.ProfileExtra` (jsonb, kod → qiymat) ni admin profil sarlavhasidagi "Ro'yxatdan o'tish
/// ma'lumotlari" kartasi uchun o'qiladigan `yorliq — qiymat` ro'yxatiga aylantiradi
/// (2026-09-26, egasining talabi: "o'quvchi kiritgan ma'lumotlar tepada ko'rinib tursin").
///
/// <para>
/// Yorliq va tanlov variantlari matni GLOBAL `RegistrationFormDefinition.CustomFields` dan
/// olinadi: tanlov javoblari bazada `Order` (butun son) sifatida saqlangan
/// (`RegistrationCustomFieldAnswers`), shu sabab ularni shu yerda `TextUz` ga qaytarmasa admin
/// faqat "2" kabi raqam ko'rardi. Superadmin maydonni keyinroq o'chirib yuborgan bo'lsa ham
/// javob YO'QOLMAYDI — yorliq o'rniga kod, qiymat xom holda ko'rsatiladi.
/// </para>
/// </summary>
internal static class StudentRegistrationExtraMapping
{
    public static IReadOnlyList<AdminStudentExtraFieldDto> Map(
        string? profileExtraJson,
        IReadOnlyList<RegistrationCustomField> fields)
    {
        if (string.IsNullOrWhiteSpace(profileExtraJson))
        {
            return [];
        }

        Dictionary<string, JsonElement> values;
        try
        {
            using var document = JsonDocument.Parse(profileExtraJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            values = document.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            // Buzuq JSON profil sahifasini yiqitmasin — shunchaki qo'shimcha maydonlar ko'rinmaydi.
            return [];
        }

        var result = new List<AdminStudentExtraFieldDto>();

        foreach (var field in fields.OrderBy(f => f.Order))
        {
            if (!values.Remove(field.Code, out var raw))
            {
                continue;
            }

            var text = FormatValue(raw, field.IsChoiceType ? field.Options : null);
            if (text is not null)
            {
                result.Add(new AdminStudentExtraFieldDto(field.Code, field.LabelUz, text));
            }
        }

        // Sozlamadan o'chirilgan maydonlarning eski javoblari — oxirida, kod bilan.
        foreach (var (code, raw) in values.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            var text = FormatValue(raw, options: null);
            if (text is not null)
            {
                result.Add(new AdminStudentExtraFieldDto(code, code, text));
            }
        }

        return result;
    }

    private static string? FormatValue(JsonElement raw, IReadOnlyList<RegistrationCustomFieldOption>? options) => raw.ValueKind switch
    {
        JsonValueKind.String => NullIfEmpty(raw.GetString()?.Trim()),
        JsonValueKind.Number => FormatNumber(raw, options),
        JsonValueKind.Array => NullIfEmpty(string.Join(", ", raw.EnumerateArray()
            .Select(item => FormatValue(item, options))
            .Where(text => text is not null))),
        JsonValueKind.True => "Ha",
        JsonValueKind.False => "Yo'q",
        _ => null,
    };

    private static string FormatNumber(JsonElement raw, IReadOnlyList<RegistrationCustomFieldOption>? options)
    {
        if (options is not null && raw.TryGetInt32(out var order))
        {
            var option = options.FirstOrDefault(o => o.Order == order);
            if (option is not null)
            {
                return option.TextUz;
            }
        }

        return raw.GetRawText();
    }

    private static string? NullIfEmpty(string? text) => string.IsNullOrEmpty(text) ? null : text;
}
