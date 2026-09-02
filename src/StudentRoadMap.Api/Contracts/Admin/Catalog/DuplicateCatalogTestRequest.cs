using StudentRoadMap.Application.Admin.Catalog.Tests.Duplicate;

namespace StudentRoadMap.Api.Contracts.Admin.Catalog;

/// <summary>`POST /api/admin/catalog/tests/{id}/duplicate` so'rov tanasi — `docs/07` §3.4: "Nusxa (`Draft`, yangi kod)".</summary>
public sealed record DuplicateCatalogTestRequest(string NewCode)
{
    public DuplicateCatalogTestCommand ToCommand(Guid id, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(id, NewCode, adminUserId, ipAddress, userAgent);
}
