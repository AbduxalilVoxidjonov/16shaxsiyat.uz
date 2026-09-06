using MediatR;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Students.List;

/// <summary>
/// `GET /api/admin/students?schoolId=&grade=&status=&needsAttention=&personalityType=&activityLevel=&from=&to=&search=&page=&pageSize=&sort=`
/// — `docs/07-api-shartnoma.md` 3.2-bo'lim. `Status` — `AssessmentStatus` nomi (masalan
/// `"Analyzed"`); boshqa filtrlar `Student` snapshot ustunlariga to'g'ridan-to'g'ri mos keladi.
/// `source` — 2026-09-06 kengaytmasi: o'quvchi maktab havolasi orqali kelganmi yoki ommaviy
/// makondan (`?source=school|public`).
/// </summary>
public sealed record ListStudentsQuery(
    Guid? SchoolId,
    int? Grade,
    string? Status,
    bool? NeedsAttention,
    string? PersonalityType,
    string? ActivityLevel,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Search,
    int Page,
    int PageSize,
    string? Sort,
    // MANBA filtri: "school" (maktab havolasi oqimi) yoki "public" (ommaviy makon).
    // `null` yoki noma'lum qiymat — filtr yo'q (hammasi), `AdminSourceFilter.Parse` ga qarang.
    string? Source = null) : IRequest<Result<PagedResult<AdminStudentListItemDto>>>;
