using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Schools.Delete;

/// <summary>
/// `DELETE /api/admin/schools/{id}` — `docs/07-api-shartnoma.md` 3.1-bo'lim: soft delete;
/// o'quvchisi bo'lsa `409 SCHOOL_HAS_STUDENTS` (`prompts/14` MAXSUS DIQQAT #5).
/// </summary>
public sealed record DeleteSchoolCommand(
    Guid Id,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result>;
