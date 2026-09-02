using MediatR;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Scales.Update;

/// <summary>`PUT /api/admin/catalog/scales/{scaleId}` — `docs/07` §3.4.</summary>
public sealed record UpdateTestScaleCommand(
    Guid ScaleId,
    string NameUz,
    string? DescriptionUz,
    int DisplayOrder,
    IReadOnlyList<InterpretationBandDto>? InterpretationBands,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<CatalogScaleItemDto>>;
