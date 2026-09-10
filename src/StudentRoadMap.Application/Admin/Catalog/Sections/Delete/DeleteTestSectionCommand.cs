using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Sections.Delete;

/// <summary>`DELETE /api/admin/catalog/sections/{sectionId}` — `docs/18` §5: savollari bo'lsa `409 SECTION_IN_USE`.</summary>
public sealed record DeleteTestSectionCommand(Guid SectionId, Guid AdminUserId, string? IpAddress = null, string? UserAgent = null) : IRequest<Result>;
