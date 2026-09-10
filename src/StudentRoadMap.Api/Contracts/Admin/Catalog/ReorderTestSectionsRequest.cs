using StudentRoadMap.Application.Admin.Catalog.Sections.Reorder;

namespace StudentRoadMap.Api.Contracts.Admin.Catalog;

/// <summary>`POST /api/admin/catalog/tests/{id}/sections/reorder` so'rov tanasi — `docs/18` §5 (`[{id, displayOrder}]`).</summary>
public sealed record ReorderTestSectionsRequest(IReadOnlyList<ReorderSectionItem> Items)
{
    public ReorderTestSectionsCommand ToCommand(Guid testDefinitionId, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(testDefinitionId, Items, adminUserId, ipAddress, userAgent);
}
