namespace StudentRoadMap.Application.Common.Exceptions;

/// <summary>
/// Optimistik konkurentlik ziddiyati — bir entity ikki bir vaqtdagi so'rov tomonidan
/// o'zgartirilganda otiladi (`docs/13-auth-va-jwt.md` QA topilmasi: TOTP asosiy kod poyga
/// holati). `Infrastructure`dagi `AppDbContext.SaveChangesAsync` EF Core'ning
/// `DbUpdateConcurrencyException`sini ushlab shu (EF'siz, portativ) istisnoga aylantiradi —
/// `Application` qatlami EF Core paketiga bog'lanmasligi uchun (`docs/06` 3-bo'lim,
/// `ValidationException`/`DomainException` bilan bir xil naqsh).
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
        : base("Ma'lumot boshqa so'rov tomonidan bir vaqtda o'zgartirildi.")
    {
    }

    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
