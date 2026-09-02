namespace StudentRoadMap.Application.Admin.Audit;

/// <summary>
/// `GET /api/admin/audit-logs` ro'yxat elementi — `docs/07-api-shartnoma.md` 3.6-bo'lim,
/// `docs/08-auth-va-xavfsizlik.md` §8: "Har yozuvda: kim, qachon, qaysi obyekt, before/after
/// (sirlarsiz), IP xeshi, user-agent". `BeforeJson`/`AfterJson` — YOZISH paytida
/// `AuditSnapshot`/har handler orqali ATAYLAB shaxsiy ma'lumotsiz shakllantirilgan
/// (`CLAUDE.md` 6-band) — bu O'QISH endpointi qo'shimcha filtrlamaydi/tozalamaydi, faqat
/// saqlangan yozuvni AYNAN qaytaradi. `AdminUserId` — SUPERADMINning o'zi (harakatni bajargan
/// admin), talaba/o'quvchi ma'lumoti EMAS.
/// </summary>
public sealed record AdminAuditLogItemDto(
    long Id,
    Guid? AdminUserId,
    string Action,
    string? EntityType,
    Guid? EntityId,
    string? BeforeJson,
    string? AfterJson,
    string? IpHash,
    string? UserAgent,
    DateTimeOffset CreatedAt);
