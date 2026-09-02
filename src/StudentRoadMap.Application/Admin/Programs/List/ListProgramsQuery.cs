using MediatR;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.List;

/// <summary>`GET /api/admin/programs?search=&status=&isActive=&page=1&pageSize=20&sort=displayOrder` — `prompts/34` E15-band.</summary>
public sealed record ListProgramsQuery(
    string? Search,
    string? Status,
    bool? IsActive,
    int Page,
    int PageSize,
    string? Sort) : IRequest<Result<PagedResult<AdminProgramListItemDto>>>;
