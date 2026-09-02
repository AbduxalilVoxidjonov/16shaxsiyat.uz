using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Archive;

/// <summary>`POST /api/admin/catalog/tests/{id}/archive` — BR-11.</summary>
public sealed record ArchiveCatalogTestCommand(Guid Id, Guid AdminUserId, string? IpAddress = null, string? UserAgent = null)
    : IRequest<Result<CatalogTestDetailDto>>;
