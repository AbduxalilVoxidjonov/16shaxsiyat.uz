using MediatR;
using StudentRoadMap.Application.Admin.Schools;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Schools.List;

/// <summary>
/// `GET /api/admin/schools?search=&region=&isActive=&page=1&pageSize=20&sort=name` —
/// `docs/07-api-shartnoma.md` 3.1-bo'lim.
/// </summary>
public sealed record ListSchoolsQuery(
    string? Search,
    string? Region,
    bool? IsActive,
    int Page,
    int PageSize,
    string? Sort) : IRequest<Result<PagedResult<AdminSchoolListItemDto>>>;
