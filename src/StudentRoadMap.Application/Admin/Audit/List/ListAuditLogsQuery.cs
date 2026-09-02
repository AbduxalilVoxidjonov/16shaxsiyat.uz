using MediatR;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Audit.List;

/// <summary>
/// `GET /api/admin/audit-logs?action=&entityType=&from=&to=&page=` — `docs/07-api-shartnoma.md`
/// 3.6-bo'lim. `PageSize` — doc jadvalida yo'q, lekin §4 umumiy pagination konvensiyasi
/// (`AdminPagingOptions` orqali baribir ≤100ga normallashadi) — boshqa ro'yxatlar bilan bir xil
/// uslub uchun qo'shildi. `Action`/`EntityType` — ANIQ moslik (`AuditActions`/entity nomi,
/// masalan `"Assessment.Deleted"`/`"Assessment"`).
/// </summary>
public sealed record ListAuditLogsQuery(
    string? Action,
    string? EntityType,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<AdminAuditLogItemDto>>>;
