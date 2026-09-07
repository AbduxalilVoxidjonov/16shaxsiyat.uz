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
/// **Shaxsiy maydonlar IXTIYORIY (2026-09-07, egasining talabi: "login qilib kirgan odamdan
/// qayta so'ralmasligi kerak").** Bitta akkaunt → bitta `Student` (`ux_students_public_user`),
/// ya'ni anketa bazada bor. Semantika:
/// • `Student` YO'Q (birinchi sessiya) — `FullName`, `BirthDate`, `Gender`, `Phone`,
///   `ConsentAccepted = true` MAJBURIY; 18 yoshgacha `ParentalConsent = true` ham;
/// • `Student` BOR — kelgan maydon yangilanish sifatida qo'llanadi (foydalanuvchi
///   "O'zgartirish" bosgan bo'lishi mumkin), kelmagani (`null`) bazadagidek qoladi.
///   `ConsentAccepted` faqat `ConsentVersion` eskirgan bo'lsa talab qilinadi;
///   `ParentalConsent` — voyaga yetmagan bo'lsa va bazada ham `false` bo'lsa.
/// Majburiylik `Student` topilganidan KEYIN aniqlanadi — shu sabab u validatorda emas,
/// handlerda (`StartPublicSessionCommandValidator` izohi).
///
/// `PublicUserId`/`IpAddress`/`UserAgent` mijozdan kelmaydi — kontroller to'ldiradi.
/// </summary>
public sealed record StartPublicSessionCommand(
    Guid PublicUserId,
    string? FullName = null,
    DateOnly? BirthDate = null,
    Gender? Gender = null,
    string? Phone = null,
    bool ConsentAccepted = false,
    /// <summary>
    /// 18 yoshgacha bo'lgan foydalanuvchi uchun `true` bo'lishi SHART. `null` — "yuborilmadi":
    /// mavjud profilda bazadagi qiymat qoladi, yangi profilda `false` deb olinadi.
    /// </summary>
    bool? ParentalConsent = null,
    /// <summary>
    /// `null` — yangi profilda "maktabda o'qimaydi" (`Student.NoGrade`), mavjud profilda
    /// "o'zgarmasin". Mavjud profilda sinfni ANIQ "yo'q" qilish uchun `0` (`Student.NoGrade`)
    /// yuboriladi — aks holda 9-sinfdan "maktabda o'qimayman"ga o'tib bo'lmasdi.
    /// </summary>
    int? Grade = null,
    /// <summary>`null` — o'zgarmasin (mavjud profil) / yo'q (yangi profil); bo'sh satr — tozalansin.</summary>
    string? Email = null,
    string? LanguageCode = null,
    string? ProgramCode = null,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<StartSessionResult>>;
