using MediatR;
using StudentRoadMap.Application.Admin.Schools;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Schools.Update;

/// <summary>
/// `PUT /api/admin/schools/{id}` — `docs/07-api-shartnoma.md` 3.1-bo'lim. `Slug`/`AccessToken`
/// bu yerda O'ZGARMAYDI (`School.UpdateDetails` domen invarianti) — havolani almashtirish
/// uchun alohida `RegenerateLink` bor.
/// </summary>
public sealed record UpdateSchoolCommand(
    Guid Id,
    string Name,
    string Region,
    string District,
    string? SchoolNumber,
    string? ContactPerson,
    string? ContactPhone,
    string? AccessCode,
    int? DailyRegistrationLimit,
    string? Notes,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null,
    /// <summary>
    /// 2026-09-23 (`docs/18` §9.7): maktabga biriktiriladigan testlar — to'plam TO'LIQ
    /// almashtiriladi. `null` — biriktirmalar o'zgarmaydi.
    /// </summary>
    IReadOnlyList<Guid>? TestIds = null) : IRequest<Result<AdminSchoolDetailDto>>;
