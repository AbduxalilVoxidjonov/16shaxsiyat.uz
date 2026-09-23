using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.PublicSpace.SetTest;

/// <summary>
/// `POST | DELETE /api/admin/public-space/tests/{testId}` (2026-09-23, `docs/07` §3.7,
/// `docs/18` §9.7) — "Dasturlar" bo'limi olib tashlangach ommaviy makonga endi TEST
/// biriktiriladi: ichkarida testning test dasturi (`TestPrograms`) — bo'lmasa yaratiladi.
/// `Linked = true` — biriktirish, `false` — olib tashlash (ikkalasi ham idempotent).
/// </summary>
public sealed record SetPublicSpaceTestCommand(
    Guid TestDefinitionId,
    bool Linked,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminPublicSpaceDto>>;
