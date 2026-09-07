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
///
/// <para>
/// **Faqat maktab sessiyalari** (egasining qarori, 2026-09-07): ommaviy makon
/// (`SchoolKind.PublicSpace`) sessiyalari bu ro'yxatga SHARTSIZ kirmaydi — ular o'z bo'limida
/// (`/admin/ommaviy` → foydalanuvchi profili) ko'rinadi. Ilgari (2026-09-06) `Source`
/// parametri bilan ixtiyoriy ajratilardi; `ListStudentsQuery` bilan bir xil sabab bilan
/// olib tashlandi. Yuborilgan `?source=` jimgina e'tiborsiz qoldiriladi (ASP.NET Core
/// noma'lum query parametrlarini xato hisoblamaydi).
/// </para>
/// </summary>
public sealed record ListAssessmentsQuery(
    Guid? SchoolId,
    string? Status,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page,
    int PageSize,
    string? Sort) : IRequest<Result<PagedResult<AdminAssessmentListItemDto>>>;
