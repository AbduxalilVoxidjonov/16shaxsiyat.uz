using MediatR;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Students.List;

/// <summary>
/// `GET /api/admin/students?schoolId=&grade=&status=&needsAttention=&personalityType=&activityLevel=&gender=&ageMin=&ageMax=&from=&to=&search=&page=&pageSize=&sort=`
/// — `docs/07-api-shartnoma.md` 3.2-bo'lim. `Status` — `AssessmentStatus` nomi (masalan
/// `"Analyzed"`); `ActivityLevel` — `ActivityLevel` nomi; `Gender` — `Male`/`Female`
/// (`ListStudentsQueryValidator`); `AgeMin`/`AgeMax` — to'liq yosh, `BirthDate` oralig'iga
/// aylantiriladi (`StudentAgeRange`). Boshqa filtrlar `Student` snapshot ustunlariga
/// to'g'ridan-to'g'ri mos keladi.
///
/// Ro'yxat FAQAT maktab o'quvchilarini qaytaradi — ommaviy makon foydalanuvchilari
/// `GET /api/admin/public-space/users` da (`AdminStudentFilterBuilder` izohi, 2026-09-07).
/// </summary>
public sealed record ListStudentsQuery(
    Guid? SchoolId,
    int? Grade,
    string? Status,
    bool? NeedsAttention,
    string? PersonalityType,
    string? ActivityLevel,
    string? Gender,
    int? AgeMin,
    int? AgeMax,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Search,
    int Page,
    int PageSize,
    string? Sort) : IRequest<Result<PagedResult<AdminStudentListItemDto>>>;
