using StudentRoadMap.Application.Admin.Catalog.Tests.Update;

namespace StudentRoadMap.Api.Contracts.Admin.Catalog;

/// <summary>`PUT /api/admin/catalog/tests/{id}` so'rov tanasi — `docs/07` §3.4.</summary>
public sealed record UpdateCatalogTestRequest(
    string NameUz,
    string? DescriptionUz,
    int DisplayOrder,
    int EstimatedMinutes,
    bool ShuffleQuestions,
    int PageSize)
{
    public UpdateCatalogTestCommand ToCommand(Guid id, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(id, NameUz, DescriptionUz, DisplayOrder, EstimatedMinutes, ShuffleQuestions, PageSize, adminUserId, ipAddress, userAgent);
}
