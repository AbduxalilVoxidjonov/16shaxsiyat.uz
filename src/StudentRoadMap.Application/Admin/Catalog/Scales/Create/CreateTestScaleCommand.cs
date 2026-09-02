using MediatR;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Scales.Create;

/// <summary>`POST /api/admin/catalog/tests/{id}/scales` — `docs/07` §3.4 (`code`, `nameUz`, `descriptionUz`, `interpretationBands`). Tizim metodikasida `409 SYSTEM_TEST_LOCKED` (domendan).</summary>
public sealed record CreateTestScaleCommand(
    Guid TestDefinitionId,
    string Code,
    string NameUz,
    string? DescriptionUz,
    int? DisplayOrder,
    IReadOnlyList<InterpretationBandDto>? InterpretationBands,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<CatalogScaleItemDto>>;
