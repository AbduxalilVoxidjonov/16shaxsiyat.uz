using StudentRoadMap.Application.Admin.Catalog.Tests.Create;

namespace StudentRoadMap.Api.Contracts.Admin.Catalog;

/// <summary>`POST /api/admin/catalog/tests` so'rov tanasi — `docs/07` §3.4.</summary>
public sealed record CreateCatalogTestRequest(
    string Code,
    string NameUz,
    string? DescriptionUz,
    int EstimatedMinutes,
    int? PageSize,
    bool? ShuffleQuestions,
    int? DisplayOrder,
    string? ScoringMode)
{
    public CreateCatalogTestCommand ToCommand(Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(Code, NameUz, DescriptionUz, EstimatedMinutes, PageSize, ShuffleQuestions, DisplayOrder, ScoringMode, adminUserId, ipAddress, userAgent);
}
