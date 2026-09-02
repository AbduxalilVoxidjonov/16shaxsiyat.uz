using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.ToggleActive;

/// <summary>`POST /api/admin/catalog/tests/{id}/toggle-active` — BR-10: faqat yangi sessiyalarga ta'sir qiladi.</summary>
public sealed record ToggleCatalogTestActiveCommand(Guid Id, Guid AdminUserId, string? IpAddress = null, string? UserAgent = null)
    : IRequest<Result<CatalogTestDetailDto>>;
