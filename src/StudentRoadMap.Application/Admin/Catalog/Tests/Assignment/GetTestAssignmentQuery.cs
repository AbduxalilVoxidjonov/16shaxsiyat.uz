using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Assignment;

/// <summary>`GET /api/admin/catalog/tests/{id}/assignment` — `docs/07` §3.4.1 (2026-09-23).</summary>
public sealed record GetTestAssignmentQuery(Guid TestDefinitionId) : IRequest<Result<AdminTestAssignmentDto>>;
