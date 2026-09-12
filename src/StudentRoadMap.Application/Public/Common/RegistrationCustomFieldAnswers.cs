using System.Text.Json;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Settings;

namespace StudentRoadMap.Application.Public.Common;

/// <summary>
/// GLOBAL ro'yxatdan o'tish formasidagi superadmin qo'shgan "o'z maydonlari"
/// (`RegistrationFormSettings.Definition.CustomFields`) javoblarini tekshiradi va
/// `Student.ProfileExtra` (jsonb) uchun shakllantiradi — P52 2-to'lqin (2026-09-12,
/// egasining qarori, `docs/18` §9.6.2).
///
/// <para>
/// Kirish shakli — `{ "KOD": qiymat }` (kod → qiymat): matn turlarida (`ShortText`/`LongText`/
/// `Phone`) satr, `SingleChoice`da BUTUN SON, `MultiChoice`da butun sonlar massivi. Butun son —
/// `RegistrationCustomFieldOption.Order` (variantning O'RNI, `Value` matni EMAS): `Value`
/// erkin matn bo'lishi mumkin (masalan localizatsiyasiz), `Order` esa barqaror butun son —
/// admin label matnini o'zgartirsa ham mijoz yuborgan javob buzilmaydi.
/// </para>
///
/// <para>
/// Validatsiya "ruhi" `SaveAnswersCommandHandler` bilan bir xil (matn uzunligi/shablon, tanlov
/// ro'yxatidan ekanligi, takrorlanmaslik) — faqat mezon manbai boshqa (`RegistrationCustomField`,
/// anketa savoli emas).
/// </para>
/// </summary>
internal static class RegistrationCustomFieldAnswers
{
    /// <summary>
    /// `fields` — GLOBAL sozlamadagi (`RegistrationFormDefinition.CustomFields`) ta'rif.
    /// `provided` — mijozdan kelgan xom qiymatlar (`null` — so'rovda `customFields` umuman
    /// yo'q). `requireMandatory` — `Required` maydon uchun qiymat yo'qligi xato hisoblansinmi
    /// (maktab oqimida DOIM `true` — boshqa asosiy maydonlar kabi har safar so'raladi; Telegram
    /// oqimida FAQAT yangi profil yaratilganda `true` — `PublicStudentProfile.RequireFields`
    /// bilan bir xil "bir marta so'raladi" naqshi).
    /// </summary>
    public static (Dictionary<string, string[]> Errors, Dictionary<string, object> Values) Validate(
        IReadOnlyList<RegistrationCustomField> fields,
        IReadOnlyDictionary<string, JsonElement>? provided,
        bool requireMandatory)
    {
        var errors = new Dictionary<string, string[]>();
        var validated = new Dictionary<string, object>();
        var source = provided ?? new Dictionary<string, JsonElement>();

        foreach (var field in fields)
        {
            // `Hidden` — mavjud naqsh (`RegistrationFieldRequirement.Hidden` izohi): mijoz
            // yuborsa ham E'TIBORSIZ qoldiriladi (saqlanmaydi).
            if (field.Requirement == RegistrationFieldRequirement.Hidden)
            {
                continue;
            }

            var hasValue = source.TryGetValue(field.Code, out var raw) && raw.ValueKind != JsonValueKind.Null;

            if (!hasValue)
            {
                if (requireMandatory && field.Requirement == RegistrationFieldRequirement.Required)
                {
                    errors[field.Code] = [$"'{field.LabelUz}' maydoni kiritilishi shart."];
                }

                continue;
            }

            var itemResult = ValidateOne(field, raw);
            if (itemResult.IsFailure)
            {
                errors[field.Code] = [itemResult.Error.Message];
                continue;
            }

            validated[field.Code] = itemResult.Value;
        }

        return (errors, validated);
    }

    /// <summary>Tasdiqlangan qiymatlarni `Student.ProfileExtra` uchun jsonb matniga aylantiradi. Bo'sh bo'lsa `null` (ustunda ortiqcha `"{}"` saqlanmasin).</summary>
    public static string? Serialize(Dictionary<string, object> validated) =>
        validated.Count == 0 ? null : JsonSerializer.Serialize(validated);

    /// <summary>
    /// Mavjud `ProfileExtra`ga YANGI tasdiqlangan qiymatlarni USTIDAN yozib birlashtiradi —
    /// so'rovda kelmagan kod bazadagidek qoladi (`IPublicProfileInput`dagi "`null` = o'zgarmasin"
    /// naqshi bilan bir xil ruh, faqat maydon darajasida). `newValues` bo'sh bo'lsa mavjud qiymat
    /// TEGILMASDAN qaytadi.
    /// </summary>
    public static string? Merge(string? existingJson, Dictionary<string, object> newValues)
    {
        if (newValues.Count == 0)
        {
            return existingJson;
        }

        var merged = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(existingJson))
        {
            try
            {
                using var document = JsonDocument.Parse(existingJson);
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    merged[property.Name] = property.Value.Clone();
                }
            }
            catch (JsonException)
            {
                // Buzuq JSON e'tiborsiz qoldiriladi — yangi qiymatlar bilan qayta yoziladi.
            }
        }

        var result = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (key, value) in merged)
        {
            result[key] = value;
        }

        foreach (var (key, value) in newValues)
        {
            result[key] = value;
        }

        return JsonSerializer.Serialize(result);
    }

    private static Result<object> ValidateOne(RegistrationCustomField field, JsonElement raw) => field.Type switch
    {
        QuestionType.ShortText or QuestionType.Phone => ValidateText(field, raw, defaultMaxLength: 200, allowPattern: true),
        QuestionType.LongText => ValidateText(field, raw, defaultMaxLength: 2000, allowPattern: false),
        QuestionType.SingleChoice => ValidateSingleChoice(field, raw),
        QuestionType.MultiChoice => ValidateMultiChoice(field, raw),
        _ => Result.Failure<object>(new Error(ProblemCodes.ValidationError, $"'{field.Code}' maydoni uchun noma'lum tur.")),
    };

    private static Result<object> ValidateText(RegistrationCustomField field, JsonElement raw, int defaultMaxLength, bool allowPattern)
    {
        if (raw.ValueKind != JsonValueKind.String)
        {
            return Result.Failure<object>(new Error(ProblemCodes.ValidationError, $"'{field.Code}' maydoni uchun matn kutilgan."));
        }

        var text = (raw.GetString() ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return Result.Failure<object>(new Error(ProblemCodes.ValidationError, $"'{field.Code}' maydoni bo'sh bo'lishi mumkin emas."));
        }

        var maxLength = field.MaxLength ?? defaultMaxLength;
        if (text.Length > maxLength)
        {
            return Result.Failure<object>(new Error(ProblemCodes.ValidationError, $"'{field.Code}' maydoni {maxLength} belgidan oshmasligi kerak."));
        }

        if (allowPattern && !string.IsNullOrWhiteSpace(field.InputPattern) && !CachedInputPatternMatcher.IsMatch(field.InputPattern, text))
        {
            return Result.Failure<object>(new Error(ProblemCodes.ValidationError, $"'{field.Code}' maydoni uchun matn kutilgan shablonga mos emas."));
        }

        return Result.Success<object>(text);
    }

    private static Result<object> ValidateSingleChoice(RegistrationCustomField field, JsonElement raw)
    {
        if (raw.ValueKind != JsonValueKind.Number || !raw.TryGetInt32(out var value))
        {
            return Result.Failure<object>(new Error(ProblemCodes.ValidationError, $"'{field.Code}' maydoni uchun butun son kutilgan."));
        }

        var options = field.Options ?? [];
        if (!options.Any(o => o.Order == value))
        {
            return Result.Failure<object>(new Error(ProblemCodes.ValidationError, $"'{field.Code}' maydoni uchun noto'g'ri variant qiymati."));
        }

        return Result.Success<object>(value);
    }

    private static Result<object> ValidateMultiChoice(RegistrationCustomField field, JsonElement raw)
    {
        if (raw.ValueKind != JsonValueKind.Array)
        {
            return Result.Failure<object>(new Error(ProblemCodes.ValidationError, $"'{field.Code}' maydoni uchun massiv kutilgan."));
        }

        var values = new List<int>();
        foreach (var element in raw.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var value))
            {
                return Result.Failure<object>(new Error(ProblemCodes.ValidationError, $"'{field.Code}' maydoni uchun butun sonlar massivi kutilgan."));
            }

            values.Add(value);
        }

        if (values.Count == 0)
        {
            return Result.Failure<object>(new Error(ProblemCodes.ValidationError, $"'{field.Code}' maydoni uchun kamida bitta variant tanlanishi kerak."));
        }

        if (values.Distinct().Count() != values.Count)
        {
            return Result.Failure<object>(new Error(ProblemCodes.ValidationError, $"'{field.Code}' maydoni uchun variantlar takrorlanmasligi kerak."));
        }

        var validOrders = (field.Options ?? []).Select(o => o.Order).ToHashSet();
        if (values.Any(v => !validOrders.Contains(v)))
        {
            return Result.Failure<object>(new Error(ProblemCodes.ValidationError, $"'{field.Code}' maydoni uchun noto'g'ri variant qiymati."));
        }

        return Result.Success<object>(values);
    }
}
