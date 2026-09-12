using System.Text.Json;
using StudentRoadMap.Application.PublicUsers.UpdateStudentProfile;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Api.Contracts.PublicUsers;

/// <summary>
/// `PUT /api/me/profile` so'rov tanasi — anketani FAQAT saqlash (sessiya ochilmaydi).
/// Maydonlar `StartPublicSessionRequest` bilan bir xil semantikada (`docs/07` §5.1b), lekin
/// `languageCode`/`programCode` YO'Q — ular sessiyaga tegishli. `PublicUserId` JWT `sub` dan,
/// `ConsentVersion` ni server qo'yadi (mijozdan qabul qilinmaydi).
///
/// Barcha maydonlar ixtiyoriy: profil bor bo'lsa `null` — "o'zgarmasin"; `grade: 0` — sinf yo'q;
/// `email: ""` — tozalash. Profil yo'q bo'lsa to'liq to'plam talab qilinadi (handler).
/// </summary>
public sealed record UpdateStudentProfileRequest(
    string? FullName = null,
    DateOnly? BirthDate = null,
    Gender? Gender = null,
    string? Phone = null,
    /// <summary>Yangi profilda yoki roziliknoma eskirganda `true` bo'lishi SHART; `null` = `false`.</summary>
    bool? ConsentAccepted = null,
    /// <summary>18 yoshgacha `true` bo'lishi SHART; `null` — mavjud qiymat qoladi.</summary>
    bool? ParentalConsent = null,
    int? Grade = null,
    string? Email = null,
    /// <summary>`IPublicProfileInput.CustomFields` izohiga qarang — tahrirda ham qo'llanadi.</summary>
    IReadOnlyDictionary<string, JsonElement>? CustomFields = null)
{
    public UpdateStudentProfileCommand ToCommand(Guid publicUserId) =>
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
            CustomFields);
}
