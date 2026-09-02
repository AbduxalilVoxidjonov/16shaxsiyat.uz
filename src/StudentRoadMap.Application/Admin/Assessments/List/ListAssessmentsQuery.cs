using MediatR;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Assessments.List;

/// <summary>
/// `GET /api/admin/assessments?schoolId=&status=&from=&to=&page=&pageSize=` —
/// `docs/07-api-shartnoma.md` 3.3-bo'lim. `Status` — `AssessmentStatus` nomi (masalan
/// `"Analyzed"`). `From`/`To` — `StartedAt` bo'yicha filtr (sessiya boshlangan sana oralig'i).
/// `Sort` — doc jadvalida yo'q, lekin §4 umumiy konvensiyasi (`Students`/`Schools` ro'yxatlari
/// bilan bir xil uslub) — ixtiyoriy, standart `-startedAt`.
/// </summary>
public sealed record ListAssessmentsQuery(
    Guid? SchoolId,
    string? Status,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page,
    int PageSize,
    string? Sort) : IRequest<Result<PagedResult<AdminAssessmentListItemDto>>>;
