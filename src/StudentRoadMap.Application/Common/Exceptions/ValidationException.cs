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
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
