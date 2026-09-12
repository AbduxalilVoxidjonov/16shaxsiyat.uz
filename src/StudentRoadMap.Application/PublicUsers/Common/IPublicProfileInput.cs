using System.Text.Json;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.PublicUsers.Common;

/// <summary>
/// Ommaviy foydalanuvchi anketasining KIRISH maydonlari — `POST /api/me/sessions`
/// (`StartPublicSessionCommand`) va `PUT /api/me/profile` (`UpdateStudentProfileCommand`)
/// uchun UMUMIY shakl. Ikkala buyruq shu interfeysni amalga oshiradi, shunda majburiylik
/// (`PublicStudentProfile.RequireFields`), tahrir (`ApplyChanges`), yaratish (`CreateStudent`)
/// va format qoidalari (`PublicProfileFormatRules`) BIR joyda yashaydi — ikki nusxa bo'lsa
/// "sessiya ochishda talab qilinadi, profil saqlashda esa yo'q" nomuvofiqligi paydo bo'lardi.
///
/// Semantika (`docs/07` §5.4 / §5.1b): `null` — "yuborilmadi": yangi profilda majburiy maydon
/// uchun xato, mavjud profilda "o'zgarmasin". `Email = ""` — tozalash, `Grade = 0` — sinf yo'q.
/// </summary>
public interface IPublicProfileInput
{
    string? FullName { get; }

    DateOnly? BirthDate { get; }

    Gender? Gender { get; }

    string? Phone { get; }

    /// <summary>Bu so'rovda rozilik berildimi — yangi profilda va eskirgan versiyada `true` SHART.</summary>
    bool ConsentAccepted { get; }

    /// <summary>`null` — yuborilmadi (mavjud qiymat qoladi; yangi profilda `false`).</summary>
    bool? ParentalConsent { get; }

    /// <summary>`null` — yangi profilda `Student.NoGrade`, mavjudida o'zgarmasin; `0` — sinfni aniq "yo'q" qilish.</summary>
    int? Grade { get; }

    /// <summary>`null` — o'zgarmasin/yo'q; bo'sh satr — tozalash.</summary>
    string? Email { get; }

    /// <summary>
    /// P52 2-to'lqin (2026-09-12, `docs/18` §9.6.2): GLOBAL ro'yxatdan o'tish formasidagi
    /// superadmin qo'shgan "o'z maydonlari" javoblari — `{ "KOD": qiymat }`
    /// (`RegistrationCustomFieldAnswers`ga qarang). Majburiylik FAQAT yangi profil
    /// yaratilganda (`existing is null`) tekshiriladi — boshqa maydonlar bilan bir xil "bir
    /// marta so'raladi" naqshi (`PublicStudentProfile.RequireFields` izohi). `null` — mijoz
    /// hech qanday o'z maydoniga javob bermagan/yubormagan.
    /// </summary>
    IReadOnlyDictionary<string, JsonElement>? CustomFields { get; }
}
