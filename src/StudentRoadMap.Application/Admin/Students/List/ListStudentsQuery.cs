using MediatR;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Students.List;

/// <summary>
/// `GET /api/admin/students?schoolId=&grade=&status=&needsAttention=&personalityType=&activityLevel=&from=&to=&search=&page=&pageSize=&sort=`
/// — `docs/07-api-shartnoma.md` 3.2-bo'lim. `Status` — `AssessmentStatus` nomi (masalan
/// `"Analyzed"`); boshqa filtrlar `Student` snapshot ustunlariga to'g'ridan-to'g'ri mos keladi.
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
    string? Sort) : IRequest<Result<PagedResult<AdminStudentListItemDto>>>;
