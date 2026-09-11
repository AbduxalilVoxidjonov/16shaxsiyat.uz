using StudentRoadMap.Application.Admin.Programs.Create;

namespace StudentRoadMap.Api.Contracts.Admin.Programs;

/// <summary>`POST /api/admin/programs` so'rov tanasi — `prompts/34` E15-band.</summary>
public sealed record CreateProgramRequest(
    string Code,
    string NameUz,
    string? DescriptionUz,
    int DisplayOrder,
    string Visibility,
    /// <summary>P52 (2026-09-11): `"Full"`/`"None"` — ixtiyoriy, standart `"Full"`.</summary>
    string RegistrationMode = "Full")
{
    public CreateProgramCommand ToCommand(Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(Code, NameUz, DescriptionUz, DisplayOrder, Visibility, adminUserId, RegistrationMode, ipAddress, userAgent);
}
