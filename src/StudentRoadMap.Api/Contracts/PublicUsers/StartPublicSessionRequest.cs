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
/// </summary>
public sealed record StartPublicSessionRequest(
    string FullName,
    DateOnly BirthDate,
    Gender Gender,
    string Phone,
    bool ConsentAccepted,
    /// <summary>18 yoshgacha bo'lganlar uchun `true` bo'lishi SHART.</summary>
    bool ParentalConsent = false,
    /// <summary>`null` — maktabda o'qimaydi; aks holda 1..11.</summary>
    int? Grade = null,
    string? Email = null,
    string? LanguageCode = null,
    /// <summary>Ommaviy makonda bitta dastur bo'lsa ixtiyoriy (maktab oqimidagi bilan bir xil qoida).</summary>
    string? ProgramCode = null)
{
    public StartPublicSessionCommand ToCommand(Guid publicUserId, string? ipAddress, string? userAgent) =>
        new(
            publicUserId,
            FullName,
            BirthDate,
            Gender,
            Phone,
            ConsentAccepted,
            ParentalConsent,
            Grade,
            Email,
            LanguageCode,
            ProgramCode,
            ipAddress,
            userAgent);
}
