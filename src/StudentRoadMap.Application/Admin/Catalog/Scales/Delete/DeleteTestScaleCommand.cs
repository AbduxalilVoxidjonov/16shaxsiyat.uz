using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Scales.Delete;

/// <summary>`DELETE /api/admin/catalog/scales/{scaleId}` — `docs/07` §3.4: "savollari bo'lsa 409".</summary>
public sealed record DeleteTestScaleCommand(Guid ScaleId, Guid AdminUserId, string? IpAddress = null, string? UserAgent = null) : IRequest<Result>;
