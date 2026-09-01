using System.Text.Json;
using FluentValidation.Results;

namespace StudentRoadMap.Application.Common.Exceptions;

/// <summary>
/// `ValidationBehavior` FluentValidation xatolarini shu istisno bilan uzatadi.
///
/// `CLAUDE.md`/`docs/06` "Natija: `Result&lt;T&gt;` — istisno biznes oqimi uchun ishlatilmaydi"
/// qoidasi handler ICHIDAGI biznes qoidalarga tegishli (masalan, dublikat sessiya, kunlik limit —
/// ular shu sabab `StartSessionCommandHandler`da `Result.Failure` bilan qaytariladi). So'rov
/// shaklini tekshiruvchi pipeline bosqichi (FluentValidation) esa handler ishga tushishidan
/// OLDIN, chegarada turadi — bu HTTP 400 ga bir xil aylanadigan, mustasno holat, shu sabab
/// istisno orqali uzatiladi (`Api/Middleware/ExceptionHandlingMiddleware` `400 VALIDATION_ERROR`
/// ga aylantiradi, `errors` maydoni bilan — `docs/06` 6-bo'lim).
///
/// **Kalit nomlash:** FluentValidation `ValidationFailure.PropertyName` C# xususiyat nomi
/// (`FullName`, `Answers[0].QuestionId`) shaklida keladi — bu `Dictionary&lt;string, string[]&gt;`
/// kalitlari sifatida saqlanganda System.Text.Json'ning `PropertyNamingPolicy`si LUG'AT
/// kalitlariga avtomatik qo'llanmaydi (faqat obyekt xususiyatlariga), shu sabab bu yerda BIR
/// MARTA, aniq `JsonNamingPolicy.CamelCase` bilan camelCase'ga o'giriladi — API'ning qolgan
/// qismi (`AddControllers`ning standart JSON siyosati) bilan bir xil bo'lishi uchun. Nuqta bilan
/// ajratilgan ichma-ich yo'l (`Student.Phone`) — har bo'lak ALOHIDA o'giriladi (`student.phone`),
/// indeksli bo'lak (`Answers[0].QuestionId`) — faqat identifikator qismi o'giriladi, `[0]` o'zgarmaydi.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException()
        : base("Bir yoki bir nechta validatsiya xatosi yuz berdi.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(f => ToCamelCasePropertyPath(f.PropertyName), f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    private static string ToCamelCasePropertyPath(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return propertyName;
        }

        var segments = propertyName.Split('.');
        for (var i = 0; i < segments.Length; i++)
        {
            segments[i] = ToCamelCaseSegment(segments[i]);
        }

        return string.Join('.', segments);
    }

    /// <summary>Bitta yo'l bo'lagini camelCase qiladi — `Answers[0]` kabi indeks qismi tegilmaydi.</summary>
    private static string ToCamelCaseSegment(string segment)
    {
        var bracketIndex = segment.IndexOf('[');
        if (bracketIndex < 0)
        {
            return JsonNamingPolicy.CamelCase.ConvertName(segment);
        }

        var identifier = segment[..bracketIndex];
        var indexer = segment[bracketIndex..];
        return JsonNamingPolicy.CamelCase.ConvertName(identifier) + indexer;
    }
}
