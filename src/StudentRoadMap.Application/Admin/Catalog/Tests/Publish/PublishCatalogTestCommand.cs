using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Publish;

/// <summary>`POST /api/admin/catalog/tests/{id}/publish` — `docs/07` §3.4.</summary>
public sealed record PublishCatalogTestCommand(Guid Id, Guid AdminUserId, string? IpAddress = null, string? UserAgent = null)
    : IRequest<Result<CatalogTestDetailDto>>;
