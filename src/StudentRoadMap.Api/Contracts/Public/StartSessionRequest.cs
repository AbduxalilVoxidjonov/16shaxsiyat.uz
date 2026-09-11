using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Api.Contracts.Public;

/// <summary>
/// `POST /api/public/sessions` so'rov tanasining ochiq (wire) shakli — `prompts/10` tuzatish #3.
/// `StartSessionCommand`dan ATAYLAB farq qiladi: `IpAddress`/`UserAgent` bu yerda YO'Q, shu sabab
/// Swagger'da mijoz yubora oladigan (lekin hech qachon ishlatilmaydigan) maydon sifatida
/// ko'rinmaydi. Bu ikkalasini kontroller (`PublicSessionController`) `HttpContext`dan o'zi
/// to'ldirib, `StartSessionCommand`ga aylantiradi (`ToCommand`).
/// </summary>
/// <summary>
/// P52 (2026-09-11): shaxs maydonlari NULLABLE — `RegistrationMode.None` dasturda mijoz
/// registratsiya ekranini umuman ko'rsatmaydi, shu maydonlarni yubormaydi.
/// </summary>
public sealed record StartSessionRequest(
    string Slug,
    string AccessToken,
    string? AccessCode,
    string? FullName,
    DateOnly? BirthDate,
    Gender? Gender,
    int? Grade,
    string? ClassLetter,
    string? Phone,
    string? ParentPhone,
    string? Email,
    bool ConsentAccepted,
    string? LanguageCode,
    /// <summary>`docs/06` 8-bo'lim (2026-09-02 qaror) — maktabda bitta dastur bo'lsa ixtiyoriy.</summary>
    string? ProgramCode = null)
{
    /// <summary>`IpAddress`/`UserAgent`ni server tomonida qo'shib, `Application` qatlami buyrug'iga aylantiradi.</summary>
    public StartSessionCommand ToCommand(string? ipAddress, string? userAgent) =>
        new(
            Slug,
            AccessToken,
            AccessCode,
            FullName,
            BirthDate,
            Gender,
            Grade,
            ClassLetter,
            Phone,
            ParentPhone,
            Email,
            ConsentAccepted,
            LanguageCode,
            ProgramCode,
            ipAddress,
            userAgent);
}
