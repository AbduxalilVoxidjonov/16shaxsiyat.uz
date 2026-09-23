using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Assignment;

/// <summary>
/// `PUT /api/admin/catalog/tests/{id}/assignment` — `docs/07` §3.4.1 (2026-09-23).
/// `SchoolIds` — maktablar to'plamini TO'LIQ almashtiradi (bo'sh ro'yxat — hammasini olib
/// tashlash). `IsInPublicSpace = null` — ommaviy makon biriktirmasi o'zgarmaydi.
/// </summary>
public sealed record UpdateTestAssignmentCommand(
    Guid TestDefinitionId,
    bool IsPublic,
    IReadOnlyList<Guid> SchoolIds,
    string RegistrationMode,
    bool? IsInPublicSpace,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminTestAssignmentDto>>;
