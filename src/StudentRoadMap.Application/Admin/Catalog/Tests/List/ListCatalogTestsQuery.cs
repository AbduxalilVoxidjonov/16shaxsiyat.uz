using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.List;

/// <summary>
/// `GET /api/admin/catalog/tests` — `docs/07` §3.4: "Barchasi: `kind`, `isSystem`, `status`,
/// savol soni, shkalalar soni". Frontend (`useCatalogTestsQuery`) sahifalashsiz TO'LIQ massiv
/// kutadi (`adminRequest&lt;CatalogTestListItem[]&gt;`) — katalog hajmi kichik (tizim 4 + admin
/// yaratgan cheklangan son), shu sabab bu yerda `PagedResult` YO'Q (Programs/Schools'dan farqli).
/// </summary>
public sealed record ListCatalogTestsQuery(string? Kind, bool? IsSystem, string? Status, string? ScoringMode)
    : IRequest<Result<IReadOnlyList<CatalogTestListItemDto>>>;
