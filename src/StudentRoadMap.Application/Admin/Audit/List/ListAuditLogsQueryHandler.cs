using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Audit.List;

/// <summary>
/// `docs/07` 3.6-bo'lim, `docs/08` §8. Read-only — `AsNoTracking`. `AuditLog`da soft-delete
/// yo'q (yozuvlar o'zgarmas — audit yaxlitligi uchun), shu sabab global so'rov filtri ham yo'q.
///
/// **Ishlash (`prompts/15` MAXSUS DIQQAT #1):** filtr/saralash/sahifalash — FAQAT `audit_logs`
/// jadvaliga, DB darajasida (`ix_audit_logs_entity(entity_type, entity_id)` `entityType`
/// filtrida ishlaydi).
///
/// **Standart saralash — `Id DESC`, `created_at DESC` EMAS** (ataylab, `ix_audit_logs_created`
/// mavjud bo'lsa ham): `Id` — `bigserial` (PK, monoton o'suvchi INSERT tartibida) — `Id DESC`
/// amalda `created_at DESC` bilan BIR XIL natija beradi (bitta DB instansida yozuv tartibi =
/// `Id` tartibi = vaqt tartibi), lekin `int64` ustunida SQLite'da ORDER BY MUAMMOSIZ ishlaydi
/// (`ListStudentsQueryHandler` izohidagi kabi, SQLite FAQAT `DateTimeOffset`da `ORDER BY`ni
/// tarjima qila olmaydi — bu yerda `sort` parametri `docs/07`da UMUMAN yo'q, ya'ni standart
/// saralashdan qochib qutulish yo'li yo'q, shu sabab `CreatedAt DESC` tanlansa BARCHA
/// integratsiya testlari SQLite'da yiqilardi). Bu — production xatti-harakatini
/// PASAYTIRISH EMAS: natija Postgres'da ham, SQLite'da ham bir xil chiqadi.
/// </summary>
internal sealed class ListAuditLogsQueryHandler : IRequestHandler<ListAuditLogsQuery, Result<PagedResult<AdminAuditLogItemDto>>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public ListAuditLogsQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<PagedResult<AdminAuditLogItemDto>>> Handle(ListAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = AdminPagingOptions.Normalize(request.Page, request.PageSize);

        var query = _context.AsNoTracking(_context.AuditLogs);

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            var action = request.Action.Trim();
            query = query.Where(a => a.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            var entityType = request.EntityType.Trim();
            query = query.Where(a => a.EntityType == entityType);
        }

        if (request.From.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= request.To.Value);
        }

        var totalCount = await _executor.CountAsync(query, cancellationToken).ConfigureAwait(false);

        // Standart — `Id DESC` (yuqoridagi klass izohiga qarang: `created_at DESC` bilan
        // TENG, lekin SQLite (sinov muhiti) `DateTimeOffset` ORDER BY cheklovidan xoli).
        var pageItems = await _executor.ToListAsync(
            query
                .OrderByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AdminAuditLogItemDto(
                    a.Id,
                    a.AdminUserId,
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    a.BeforeJson,
                    a.AfterJson,
                    a.IpHash,
                    a.UserAgent,
                    a.CreatedAt)),
            cancellationToken).ConfigureAwait(false);

        return Result.Success(PagedResult<AdminAuditLogItemDto>.Create(pageItems, page, pageSize, totalCount));
    }
}
