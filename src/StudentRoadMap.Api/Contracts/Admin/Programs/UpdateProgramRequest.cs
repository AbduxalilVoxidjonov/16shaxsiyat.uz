using StudentRoadMap.Application.Admin.Programs.Update;

namespace StudentRoadMap.Api.Contracts.Admin.Programs;

/// <summary>`PUT /api/admin/programs/{id}` so'rov tanasi — `prompts/34` E15-band.</summary>
public sealed record UpdateProgramRequest(
    string NameUz,
    string? DescriptionUz,
    int DisplayOrder,
    string Visibility)
{
    public UpdateProgramCommand ToCommand(Guid id, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(id, NameUz, DescriptionUz, DisplayOrder, Visibility, adminUserId, ipAddress, userAgent);
}
