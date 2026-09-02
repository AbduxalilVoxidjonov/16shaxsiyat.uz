using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Application.Admin.Catalog.Scales.Create;

namespace StudentRoadMap.Api.Contracts.Admin.Catalog;

/// <summary>`POST /api/admin/catalog/tests/{id}/scales` so'rov tanasi — `docs/07` §3.4 (`code`, `nameUz`, `descriptionUz`, `interpretationBands`).</summary>
public sealed record CreateTestScaleRequest(
    string Code,
    string NameUz,
    string? DescriptionUz,
    int? DisplayOrder,
    IReadOnlyList<InterpretationBandDto>? InterpretationBands)
{
    public CreateTestScaleCommand ToCommand(Guid testDefinitionId, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(testDefinitionId, Code, NameUz, DescriptionUz, DisplayOrder, InterpretationBands, adminUserId, ipAddress, userAgent);
}
