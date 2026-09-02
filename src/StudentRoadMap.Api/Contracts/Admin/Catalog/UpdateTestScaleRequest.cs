using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Application.Admin.Catalog.Scales.Update;

namespace StudentRoadMap.Api.Contracts.Admin.Catalog;

/// <summary>`PUT /api/admin/catalog/scales/{scaleId}` so'rov tanasi — `docs/07` §3.4.</summary>
public sealed record UpdateTestScaleRequest(
    string NameUz,
    string? DescriptionUz,
    int DisplayOrder,
    IReadOnlyList<InterpretationBandDto>? InterpretationBands)
{
    public UpdateTestScaleCommand ToCommand(Guid scaleId, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(scaleId, NameUz, DescriptionUz, DisplayOrder, InterpretationBands, adminUserId, ipAddress, userAgent);
}
