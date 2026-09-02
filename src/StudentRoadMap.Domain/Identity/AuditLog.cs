namespace StudentRoadMap.Domain.Identity;

/// <summary>
/// Audit yozuvi — kim, qachon, qaysi harakatni bajargani (`docs/05-database-schema.md`
/// `audit_logs` jadvali, `docs/08-auth-va-xavfsizlik.md` 8-bo'lim). `Id` — `bigserial`
/// (DB tomonidan generatsiya qilinadi), shu sabab bu klass `Entity`/`AggregateRoot`
/// bazaviy sinflaridan meros OLMAYDI (ular `Guid Id` talab qiladi) — xuddi
/// `RegistrationCounter`dagi kabi (`docs/05` DDL: `id bigserial PRIMARY KEY`).
/// </summary>
public sealed class AuditLog
{
    public long Id { get; private set; }

    public Guid? AdminUserId { get; private set; }

    public string Action { get; private set; } = null!;

    public string? EntityType { get; private set; }

    public Guid? EntityId { get; private set; }

    /// <summary>O'zgarishdan OLDINGI holat — `jsonb`, sirlarsiz (`docs/08` 8-bo'lim).</summary>
    public string? BeforeJson { get; private set; }

    /// <summary>O'zgarishdan KEYINGI holat — `jsonb`, sirlarsiz.</summary>
    public string? AfterJson { get; private set; }

    /// <summary>Xom IP emas — `IIpHasher` orqali xeshlangan (`CLAUDE.md` 9-band).</summary>
    public string? IpHash { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private AuditLog()
    {
    }

    private AuditLog(
        Guid? adminUserId,
        string action,
        string? entityType,
        Guid? entityId,
        string? beforeJson,
        string? afterJson,
        string? ipHash,
        string? userAgent,
        DateTimeOffset now)
    {
        AdminUserId = adminUserId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        BeforeJson = beforeJson;
        AfterJson = afterJson;
        IpHash = ipHash;
        UserAgent = userAgent;
        CreatedAt = now;
    }

    public static AuditLog Create(
        string action,
        DateTimeOffset now,
        Guid? adminUserId = null,
        string? entityType = null,
        Guid? entityId = null,
        string? beforeJson = null,
        string? afterJson = null,
        string? ipHash = null,
        string? userAgent = null)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Audit harakat nomi bo'sh bo'lishi mumkin emas.", nameof(action));
        }

        return new AuditLog(adminUserId, action, entityType, entityId, beforeJson, afterJson, ipHash, userAgent, now);
    }
}
