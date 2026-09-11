using StudentRoadMap.Application.Admin.Programs.Update;
using StudentRoadMap.Application.Common.Models;

namespace StudentRoadMap.Api.Contracts.Admin.Programs;

/// <summary>`PUT /api/admin/programs/{id}` so'rov tanasi — `prompts/34` E15-band.</summary>
public sealed record UpdateProgramRequest(
    string NameUz,
    string? DescriptionUz,
    int DisplayOrder,
    string Visibility,
    /// <summary>P52 (2026-09-11): `"Full"`/`"None"` — ixtiyoriy, standart `"Full"`.</summary>
    string RegistrationMode = "Full",
    /// <summary>
    /// P52 kengaytmasi (2026-09-11, `docs/18` §9.5): TO'LIQ ALMASHTIRISH — `null`
    /// (yubormaslik) standart qiymatlarga qaytaradi (`RegistrationMode` bilan bir xil naqsh).
    /// </summary>
    RegistrationFieldsInput? RegistrationFields = null)
{
    public UpdateProgramCommand ToCommand(Guid id, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(id, NameUz, DescriptionUz, DisplayOrder, Visibility, adminUserId, RegistrationMode, RegistrationFields, ipAddress, userAgent);
}
