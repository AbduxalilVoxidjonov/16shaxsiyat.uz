using System.Text.Json;
using StudentRoadMap.Application.Public.StartPublicSession;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Api.Contracts.PublicUsers;

/// <summary>
/// `POST /api/me/sessions` so'rov tanasi. `PublicUserId` bu yerda YO'Q — u JWT `sub`
/// claim'idan olinadi (`CLAUDE.md` 8-qoida ruhida: egalik hech qachon tanadan kelmaydi),
/// `IpAddress`/`UserAgent` esa `HttpContext`dan (`StartSessionRequest` bilan bir xil naqsh).
///
/// `ConsentVersion` ham ATAYLAB YO'Q — uni server qo'yadi (`PublicConsent.CurrentVersion`),
/// aks holda foydalanuvchi "qaysi matnga rozilik berdim" yozuvini soxtalashtira olardi.
///
/// **Barcha shaxsiy maydonlar IXTIYORIY** (`docs/07` §5.4, 2026-09-07): profil bazada bo'lsa
/// bo'sh tana `{}` (yoki faqat `programCode`) yetarli. Majburiylik profil holatiga qarab
/// handlerda aniqlanadi (`StartPublicSessionCommand` izohi).
/// </summary>
public sealed record StartPublicSessionRequest(
    string? FullName = null,
    DateOnly? BirthDate = null,
    Gender? Gender = null,
    string? Phone = null,
    /// <summary>
    /// Yangi profilda yoki roziliknoma versiyasi eskirganda `true` bo'lishi SHART. `bool?` —
    /// swagger'da ixtiyoriy bo'lib chiqishi uchun: profil bor mijoz `{}`/`{programCode}`
    /// yuboradi, `null` server uchun `false` bilan bir xil (rozilik bu safar berilmadi).
    /// </summary>
    bool? ConsentAccepted = null,
    /// <summary>18 yoshgacha bo'lganlar uchun `true` bo'lishi SHART; `null` — mavjud qiymat qoladi.</summary>
    bool? ParentalConsent = null,
    /// <summary>`null` — yangi profilda "sinf yo'q", mavjud profilda "o'zgarmasin"; `0` — sinfni aniq "yo'q" qilish; aks holda 1..11.</summary>
    int? Grade = null,
    /// <summary>`null` — o'zgarmasin/yo'q; bo'sh satr — mavjud emailni tozalash.</summary>
    string? Email = null,
    string? LanguageCode = null,
    /// <summary>Ommaviy makonda bitta dastur bo'lsa ixtiyoriy (maktab oqimidagi bilan bir xil qoida).</summary>
    string? ProgramCode = null,
    /// <summary>`IPublicProfileInput.CustomFields` izohiga qarang — FAQAT profil yaratilayotganda so'raladi.</summary>
    IReadOnlyDictionary<string, JsonElement>? CustomFields = null)
{
    public StartPublicSessionCommand ToCommand(Guid publicUserId, string? ipAddress, string? userAgent) =>
        new(
            publicUserId,
            FullName,
            BirthDate,
            Gender,
            Phone,
            ConsentAccepted ?? false,
            ParentalConsent,
            Grade,
            Email,
            LanguageCode,
            ProgramCode,
            ipAddress,
            userAgent,
            CustomFields);
}
