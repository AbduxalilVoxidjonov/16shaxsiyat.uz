using MediatR;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Sections.Reorder;

/// <summary>`POST /api/admin/catalog/tests/{id}/sections/reorder` — `docs/18` §5 (`[{id, displayOrder}]`).</summary>
public sealed record ReorderSectionItem(Guid Id, int DisplayOrder);

public sealed record ReorderTestSectionsCommand(
    Guid TestDefinitionId,
    IReadOnlyList<ReorderSectionItem> Items,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<IReadOnlyList<CatalogSectionItemDto>>>;
