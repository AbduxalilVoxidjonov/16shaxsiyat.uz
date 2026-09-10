using MediatR;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Sections.List;

/// <summary>`GET /api/admin/catalog/tests/{id}/sections` — `docs/18` §5.</summary>
public sealed record ListTestSectionsQuery(Guid TestDefinitionId) : IRequest<Result<IReadOnlyList<CatalogSectionItemDto>>>;
