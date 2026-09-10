using MediatR;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Domain.Catalog.Branching;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Sections.Create;

/// <summary>
/// `POST /api/admin/catalog/tests/{id}/sections` — `docs/18` §5. Tizim metodikasida
/// `409 SYSTEM_TEST_LOCKED` (domendan, B-3/BR-8). `Visibility` berilib `ScoringMode = Scored`
/// bo'lsa `400 BRANCHING_NOT_ALLOWED_IN_SCORED` (B-2, domendan).
/// </summary>
public sealed record CreateTestSectionCommand(
    Guid TestDefinitionId,
    string Code,
    string TitleUz,
    string? DescriptionUz,
    int? DisplayOrder,
    VisibilityRule? Visibility,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<CatalogSectionItemDto>>;
