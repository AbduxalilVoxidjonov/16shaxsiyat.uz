using MediatR;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Scales.List;

/// <summary>`GET /api/admin/catalog/tests/{id}/scales` — `docs/07` §3.4.</summary>
public sealed record ListTestScalesQuery(Guid TestDefinitionId) : IRequest<Result<IReadOnlyList<CatalogScaleItemDto>>>;
