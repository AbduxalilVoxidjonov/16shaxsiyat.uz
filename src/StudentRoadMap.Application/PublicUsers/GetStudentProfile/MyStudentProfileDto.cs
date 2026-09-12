using StudentRoadMap.Application.Admin.Settings.RegistrationForm;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.PublicUsers.GetStudentProfile;

/// <summary>
/// `GET /api/me/profile` javobi (`docs/07` §5.1a) — ommaviy foydalanuvchining SAQLANGAN
/// anketasi (`Student`, `PublicUserId` bo'yicha). `GetProfile/PublicUserDto` (`GET /api/me`)
/// bilan ARALASHTIRILMAYDI: u Telegram akkaunti (ism, rasm), bu esa test anketasi (F.I.Sh.,
/// tug'ilgan sana, telefon, rozilik holati).
///
/// `HasProfile = false` bo'lsa shaxsiy maydonlar `null`, `SuggestedFullName` esa Telegram
/// `LastName + FirstName` dan TAKLIF — foydalanuvchi tahrirlaydi (Telegram ismi ko'pincha
/// rasmiy F.I.Sh. emas). Hech qanday identifikator (`Student.Id`, `TelegramId`) qaytarilmaydi —
/// `CLAUDE.md` 8-qoida: egalik JWT bilan, ID mijozga kerak emas.
/// </summary>
public sealed record MyStudentProfileDto(
    bool HasProfile,
    string? FullName,
    DateOnly? BirthDate,
    Gender? Gender,
    string? Phone,
    /// <summary>`null` — maktabda o'qimaydi (`Student.NoGrade` mijozga chiqmaydi).</summary>
    int? Grade,
    string? Email,
    string? ConsentVersion,
    /// <summary>`Student.ConsentVersion` joriy roziliknoma versiyasiga tengmi — `false` bo'lsa anketa rozilikni qayta so'raydi.</summary>
    bool ConsentCurrent,
    bool ParentalConsent,
    /// <summary>Yosh 18 dan kichikmi (`Student.CalculateAge`) — profil bo'lmasa `false`.</summary>
    bool IsMinor,
    string? SuggestedFullName,
    /// <summary>
    /// GLOBAL ro'yxatdan o'tish formasi ta'rifi (P52 2-to'lqin, 2026-09-12, `docs/18` §9.6.2) —
    /// superadmin qo'shgan "o'z maydonlari" bilan birga, mijoz forma chizishi uchun. Dastur
    /// ustunligi (batareya) BU YERDA QO'LLANMAYDI — profil dastur tanlanishidan OLDIN
    /// so'raladi (`RegistrationFormResolver` sinf izohiga qarang).
    /// </summary>
    RegistrationFormDefinitionDto RegistrationForm);
