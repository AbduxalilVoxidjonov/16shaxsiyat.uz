using MediatR;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.List;

/// <summary>
/// `GET /api/admin/programs?search=&state=&page=1&pageSize=20&sort=displayOrder` — `prompts/34` E15-band.
///
/// **2026-09-06:** ilgari ikkita mustaqil filtr bor edi (`status` va `isActive`) va ular
/// bir-birini inkor qiladigan juftlikni tanlash imkonini berardi (masalan
/// `status=Archived&amp;isActive=true`). Endi BITTA `state` filtri
/// (`Draft` · `Active` · `Paused` · `Archived`) — mumkin bo'lgan har bir kombinatsiya aynan
/// bitta qiymat bilan ifodalanadi. Eski parametrlar SAQLANMADI: ular endi ma'nosiz
/// (`status=Published` "faolmi yoki to'xtatilganmi" degan savolga javob bermaydi) va bu
/// ichki admin API'ning yagona mijozi — shu repodagi frontend.
/// </summary>
public sealed record ListProgramsQuery(
    string? Search,
    string? State,
    int Page,
    int PageSize,
    string? Sort) : IRequest<Result<PagedResult<AdminProgramListItemDto>>>;
