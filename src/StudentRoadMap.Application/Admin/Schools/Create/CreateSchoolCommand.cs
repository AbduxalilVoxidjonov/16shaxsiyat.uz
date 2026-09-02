using MediatR;
using StudentRoadMap.Application.Admin.Schools;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Schools.Create;

/// <summary>
/// `POST /api/admin/schools` — `docs/07-api-shartnoma.md` 3.1-bo'lim. `Slug` avtomatik
/// generatsiya qilinadi (`Name` + `District` → translit, `prompts/14` MAXSUS DIQQAT #3),
/// so'rovda qabul qilinmaydi.
/// </summary>
public sealed record CreateSchoolCommand(
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
    string? UserAgent = null) : IRequest<Result<AdminSchoolDetailDto>>;
