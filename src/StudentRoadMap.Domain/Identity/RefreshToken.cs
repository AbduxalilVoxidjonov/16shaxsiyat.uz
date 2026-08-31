using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Identity;

/// <summary>JWT yangilash tokeni (`docs/04` 2.10-bo'lim).</summary>
public sealed class RefreshToken : Entity
{
    public Guid AdminUserId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public string? CreatedByIpHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private RefreshToken()
    {
    }

    private RefreshToken(Guid id, Guid adminUserId, string tokenHash, DateTimeOffset expiresAt, string? createdByIpHash, DateTimeOffset now)
        : base(id)
    {
        AdminUserId = adminUserId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedByIpHash = createdByIpHash;
        CreatedAt = now;
    }

    public static RefreshToken Create(Guid id, Guid adminUserId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset now, string? createdByIpHash = null)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Token xeshi bo'sh bo'lishi mumkin emas.", nameof(tokenHash));
        }

        if (expiresAt <= now)
        {
            throw new ArgumentException("Amal qilish muddati joriy vaqtdan keyin bo'lishi kerak.", nameof(expiresAt));
        }

        return new RefreshToken(id, adminUserId, tokenHash, expiresAt, createdByIpHash, now);
    }

    public void Revoke(DateTimeOffset now)
    {
        if (RevokedAt is not null)
        {
            throw new DomainException("REFRESH_TOKEN_ALREADY_REVOKED", "Token allaqachon bekor qilingan.");
        }

        RevokedAt = now;
    }
}
