namespace StudentRoadMap.Domain.Common;

/// <summary>
/// Muvaffaqiyatsiz natija xatosi — `code` maydoni `docs/06-arxitektura.md` 6-bo'limidagi
/// xato kodlari uslubiga mos (masalan, `VALIDATION_ERROR`, `SYSTEM_TEST_LOCKED`).
/// </summary>
public sealed class Error : IEquatable<Error>
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public string Code { get; }

    public string Message { get; }

    /// <summary>
    /// Standart `code`/`message`dan tashqari, javobga qo'shiladigan qo'shimcha maydonlar
    /// (masalan `docs/07` 1.7-bo'lim: `400 VALIDATION_ERROR` + `unansweredCount`). `null` —
    /// odatiy hol, aksariyat xatolarda qo'shimcha maydon yo'q. Tenglikka (`Equals`) kirmaydi —
    /// faqat javobni boyitish uchun, xato identifikatorini o'zgartirmaydi.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Extensions { get; }

    public Error(string code, string message, IReadOnlyDictionary<string, object>? extensions = null)
    {
        Code = code;
        Message = message;
        Extensions = extensions;
    }

    public bool Equals(Error? other)
    {
        if (other is null)
        {
            return false;
        }

        return Code == other.Code && Message == other.Message;
    }

    public override bool Equals(object? obj) => Equals(obj as Error);

    public override int GetHashCode() => HashCode.Combine(Code, Message);
}
