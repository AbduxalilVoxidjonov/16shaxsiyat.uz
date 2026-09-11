using StudentRoadMap.Application.Admin.Programs.Create;
using StudentRoadMap.Application.Common.Models;

namespace StudentRoadMap.Api.Contracts.Admin.Programs;

/// <summary>`POST /api/admin/programs` so'rov tanasi — `prompts/34` E15-band.</summary>
public sealed record CreateProgramRequest(
    string Code,
    string NameUz,
    string? DescriptionUz,
    int DisplayOrder,
    string Visibility,
    /// <summary>P52 (2026-09-11): `"Full"`/`"None"` — ixtiyoriy, standart `"Full"`.</summary>
    string RegistrationMode = "Full",
    /// <summary>P52 kengaytmasi (2026-09-11, `docs/18` §9.5): ixtiyoriy — `null` standart qiymatlarni bildiradi.</summary>
    RegistrationFieldsInput? RegistrationFields = null)
{
    public CreateProgramCommand ToCommand(Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(Code, NameUz, DescriptionUz, DisplayOrder, Visibility, adminUserId, RegistrationMode, RegistrationFields, ipAddress, userAgent);
}
