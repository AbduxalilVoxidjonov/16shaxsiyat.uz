using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.PublicUsers;

/// <summary>
/// Ommaviy foydalanuvchining JWT yangilash tokeni (`docs/04` 2.12-bo'lim).
///
/// `Identity/RefreshToken` (superadmin) NAQSHINI AYNAN takrorlaydi — SHA-256 xeshlangan token,
/// rotatsiya (har `refresh` da eskisi `Revoke`), qayta-ishlatishni aniqlash (`RevokedAt is not null`
/// bo'lgan token bilan kelish = o'g'irlik belgisi → foydalanuvchining barcha tokenlari bekor
/// qilinadi), muddat. Yagona farq — FK `PublicUserId` (superadminniki `AdminUserId`).
///
/// Nima uchun mavjud `RefreshToken` polimorfik qilinmadi: uning FK'si `admin_users` ga
/// `Cascade` bilan bog'langan va superadmin xavfsizlik yuzasi (TOTP, hisob blokirovkasi,
/// audit) butunlay boshqacha. Ikkita nullable FK bilan bitta jadvalga siqish har ikki oqimda
/// ham "ikkalasi ham NULL" holatini domen darajasida tekshirishni talab qilardi va
/// superadmin jadvalini tashqi trafikka ochib qo'yardi.
/// </summary>
public sealed class PublicRefreshToken : Entity
{
    public Guid PublicUserId { get; private set; }

    /// <summary>Xom token HECH QACHON saqlanmaydi — faqat SHA-256 xeshi (kichik harfli hex, 64 belgi).</summary>
    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public string? CreatedByIpHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private PublicRefreshToken()
    {
    }

    private PublicRefreshToken(Guid id, Guid publicUserId, string tokenHash, DateTimeOffset expiresAt, string? createdByIpHash, DateTimeOffset now)
        : base(id)
    {
        PublicUserId = publicUserId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedByIpHash = createdByIpHash;
        CreatedAt = now;
    }

    public static PublicRefreshToken Create(Guid id, Guid publicUserId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset now, string? createdByIpHash = null)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Token xeshi bo'sh bo'lishi mumkin emas.", nameof(tokenHash));
        }

        if (expiresAt <= now)
        {
            throw new ArgumentException("Amal qilish muddati joriy vaqtdan keyin bo'lishi kerak.", nameof(expiresAt));
        }

        return new PublicRefreshToken(id, publicUserId, tokenHash, expiresAt, createdByIpHash, now);
    }

    public void Revoke(DateTimeOffset now)
    {
        if (RevokedAt is not null)
        {
            throw new DomainException("PUBLIC_REFRESH_TOKEN_ALREADY_REVOKED", "Token allaqachon bekor qilingan.");
        }

        RevokedAt = now;
    }
}
