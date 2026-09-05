using MediatR;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Public.StartPublicSession;

/// <summary>
/// `POST /api/me/sessions` — ommaviy (MAKTABSIZ) sessiya ochish. Maktab oqimidan farqi:
/// `slug`/`accessToken` YO'Q, egalik JWT (`PublicUserId`) bilan aniqlanadi, makon esa
/// har doim yagona `SchoolKind.PublicSpace`.
///
/// **Nima uchun `StartSessionCommand` KENGAYTIRILMADI, balki alohida buyruq yozildi:**
/// vazifa shartining eng qat'iy talabi — "maktab oqimining xatti-harakati bir belgi ham
/// o'zgarmasin" (242 integratsiya testi). Ikki oqimning KIRISH shartnomasi bir-biriga
/// mos kelmaydi:
/// • maktab: `slug` + `accessToken` MAJBURIY, yosh 6–20, sinf 1–11 MAJBURIY, JWT yo'q;
/// • ommaviy: JWT MAJBURIY, `slug`/`accessToken` MA'NOSIZ, yosh 6–99 (`Student.MinAge`/
///   `MaxAge`), sinf IXTIYORIY (`Student.NoGrade` — maktabda o'qimaydigan kattalar).
/// Bitta buyruqqa siqish `StartSessionCommandValidator` ni "agar slug bo'lsa..." shartlariga
/// to'ldirishni talab qilardi — ya'ni maktab oqimining validatsiya yo'lini o'zgartirardi.
/// Umumiy qism esa nusxalanmadi: `ProgramAvailability` va `AssessmentTestAttacher` ikkala
/// handler tomonidan BIR XIL ishlatiladi.
///
/// `PublicUserId`/`IpAddress`/`UserAgent` mijozdan kelmaydi — kontroller to'ldiradi.
/// </summary>
public sealed record StartPublicSessionCommand(
    Guid PublicUserId,
    string FullName,
    DateOnly BirthDate,
    Gender Gender,
    string Phone,
    bool ConsentAccepted,
    /// <summary>18 yoshgacha bo'lgan foydalanuvchi uchun MAJBURIY (`StartPublicSessionCommandValidator`).</summary>
    bool ParentalConsent = false,
    /// <summary>`null` — maktabda o'qimaydi (`Student.NoGrade`); aks holda 1..11.</summary>
    int? Grade = null,
    string? Email = null,
    string? LanguageCode = null,
    string? ProgramCode = null,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<StartSessionResult>>;
