using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Duplicate;

/// <summary>`POST /api/admin/catalog/tests/{id}/duplicate` — `docs/07` §3.4: "Nusxa (`Draft`, yangi kod)". `TestDefinition.Duplicate` doim `Custom`/`IsSystem=false` nusxa yaratadi — tizim metodikasini ham tahrirlash uchun nusxalash mumkin.</summary>
public sealed record DuplicateCatalogTestCommand(Guid Id, string NewCode, Guid AdminUserId, string? IpAddress = null, string? UserAgent = null)
    : IRequest<Result<CatalogTestDetailDto>>;
