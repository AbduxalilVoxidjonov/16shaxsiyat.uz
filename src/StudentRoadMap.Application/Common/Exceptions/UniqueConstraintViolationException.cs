namespace StudentRoadMap.Application.Common.Exceptions;

/// <summary>
/// DB darajasidagi unique cheklov (masalan `ux_schools_slug`) buzilganda otiladi —
/// `ConcurrencyConflictException` bilan bir xil naqsh (`docs/06` 3-bo'lim: `Application`
/// EF Core paketiga bog'lanmaydi, shu sabab `Infrastructure`dagi `AppDbContext.SaveChangesAsync`
/// EF Core'ning umumiy `DbUpdateException`sini shu portativ istisnoga aylantiradi).
///
/// **Qachon ishlatiladi (`prompts/14` MAXSUS DIQQAT #3):** maktab slug generatsiyasida poyga
/// holati — ikki admin bir vaqtda bir xil nomli maktab yarata olsa. `CreateSchoolCommandHandler`
/// oldindan (proaktiv) bo'sh slug qidiradi (`-2`, `-3`, ...), bu holatlarning 99%+ ini yopadi;
/// bu istisno faqat ChIN bir vaqtdagi (ikkala so'rov ORALIQ tekshiruv bilan YAKUNIY yozuv
/// orasida) poyga holatini qamraydi — `ExceptionHandlingMiddleware` uni tushunarli `409`ga
/// aylantiradi (jimgina `500` emas), mijoz qaytadan urinib ko'rishi kerak.
/// </summary>
public sealed class UniqueConstraintViolationException : Exception
{
    public string Code { get; }

    public UniqueConstraintViolationException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public UniqueConstraintViolationException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}
