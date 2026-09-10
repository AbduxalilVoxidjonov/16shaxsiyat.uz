using StudentRoadMap.Application.Admin.Catalog.Sections.Update;
using StudentRoadMap.Domain.Catalog.Branching;

namespace StudentRoadMap.Api.Contracts.Admin.Catalog;

/// <summary>`PUT /api/admin/catalog/sections/{sectionId}` so'rov tanasi — `docs/18` §5. `code` o'zgarmaydi.</summary>
public sealed record UpdateTestSectionRequest(
    string TitleUz,
    string? DescriptionUz,
    VisibilityRule? Visibility)
{
    public UpdateTestSectionCommand ToCommand(Guid sectionId, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(sectionId, TitleUz, DescriptionUz, Visibility, adminUserId, ipAddress, userAgent);
}
