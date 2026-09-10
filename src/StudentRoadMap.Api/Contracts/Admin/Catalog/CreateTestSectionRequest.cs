using StudentRoadMap.Application.Admin.Catalog.Sections.Create;
using StudentRoadMap.Domain.Catalog.Branching;

namespace StudentRoadMap.Api.Contracts.Admin.Catalog;

/// <summary>`POST /api/admin/catalog/tests/{id}/sections` so'rov tanasi — `docs/18` §5 (`{ code, titleUz, descriptionUz, displayOrder, visibility }`).</summary>
public sealed record CreateTestSectionRequest(
    string Code,
    string TitleUz,
    string? DescriptionUz,
    int? DisplayOrder,
    VisibilityRule? Visibility)
{
    public CreateTestSectionCommand ToCommand(Guid testDefinitionId, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(testDefinitionId, Code, TitleUz, DescriptionUz, DisplayOrder, Visibility, adminUserId, ipAddress, userAgent);
}
