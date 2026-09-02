using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Delete;

/// <summary>`DELETE /api/admin/catalog/tests/{id}` — `docs/07` §3.4: "Faqat `Custom` + hech qaysi sessiyada ishlatilmagan".</summary>
public sealed record DeleteCatalogTestCommand(Guid Id, Guid AdminUserId, string? IpAddress = null, string? UserAgent = null) : IRequest<Result>;
