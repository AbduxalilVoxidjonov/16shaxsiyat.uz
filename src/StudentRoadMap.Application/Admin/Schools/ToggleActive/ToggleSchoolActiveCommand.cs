using MediatR;
using StudentRoadMap.Application.Admin.Schools;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Schools.ToggleActive;

/// <summary>
/// `POST /api/admin/schools/{id}/toggle-active` — `docs/07-api-shartnoma.md` 3.1-bo'lim.
/// Joriy holatning teskarisiga o'tkazadi (faol → nofaol, nofaol → faol).
/// </summary>
public sealed record ToggleSchoolActiveCommand(
    Guid Id,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminSchoolDetailDto>>;
