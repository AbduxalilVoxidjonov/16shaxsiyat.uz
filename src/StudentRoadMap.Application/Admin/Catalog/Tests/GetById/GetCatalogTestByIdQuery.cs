using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.GetById;

/// <summary>`GET /api/admin/catalog/tests/{id}` — `docs/07` §3.4.</summary>
public sealed record GetCatalogTestByIdQuery(Guid Id) : IRequest<Result<CatalogTestDetailDto>>;
