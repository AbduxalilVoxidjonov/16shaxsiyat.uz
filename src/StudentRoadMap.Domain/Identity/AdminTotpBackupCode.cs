using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Identity;

/// <summary>
/// TOTP zaxira kodi — `docs/08-auth-va-xavfsizlik.md` 2-bo'lim: "Yoqilganda 8 ta bir martalik
/// zaxira kod beriladi (xeshlangan holda saqlanadi)". `docs/05-database-schema.md` da alohida
/// jadval yo'q edi (faqat `AdminUser.TotpSecretEncrypted` bor) — audit_logs kabi, bu ham P13
/// uchun yangi, kerakli jadval (hisobotga qarang). `CodeHash` — `IPasswordHasher` orqali
/// (tuzli, qaytarilmas) xeshlangan, xom kod saqlanmaydi.
/// </summary>
public sealed class AdminTotpBackupCode : Entity
{
    public Guid AdminUserId { get; private set; }

    public string CodeHash { get; private set; } = null!;

    public DateTimeOffset? UsedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsUsed => UsedAt is not null;

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private AdminTotpBackupCode()
    {
    }

    private AdminTotpBackupCode(Guid id, Guid adminUserId, string codeHash, DateTimeOffset now)
        : base(id)
    {
        AdminUserId = adminUserId;
        CodeHash = codeHash;
        CreatedAt = now;
    }

    public static AdminTotpBackupCode Create(Guid id, Guid adminUserId, string codeHash, DateTimeOffset now)
    {
        if (adminUserId == Guid.Empty)
        {
            throw new ArgumentException("Admin identifikatori bo'sh bo'lishi mumkin emas.", nameof(adminUserId));
        }

        if (string.IsNullOrWhiteSpace(codeHash))
        {
            throw new ArgumentException("Kod xeshi bo'sh bo'lishi mumkin emas.", nameof(codeHash));
        }

        return new AdminTotpBackupCode(id, adminUserId, codeHash, now);
    }

    /// <summary>
    /// Domen sathida to'g'ri ifodalangan mutatsiya — testlarda va kelajakdagi admin-panel
    /// oqimlarida (masalan, qo'lda bekor qilish) ishlatiladi. **Login oqimi buni chaqirmaydi**:
    /// EF tracked "o'qi-tekshir-yoz" poyga holatiga ochiq bo'lgani uchun (QA topilmasi,
    /// `docs/13-auth-va-jwt.md`) `LoginCommandHandler` o'rniga `IAppDbContext.TryMarkTotpBackupCodeUsedAsync`
    /// (atomik `UPDATE ... WHERE used_at IS NULL`, `RegistrationCounter`dagi bilan bir xil
    /// sabab/naqsh) ishlatadi.
    /// </summary>
    public void MarkUsed(DateTimeOffset now)
    {
        if (UsedAt is not null)
        {
            throw new DomainException("TOTP_BACKUP_CODE_ALREADY_USED", "Zaxira kod allaqachon ishlatilgan.");
        }

        UsedAt = now;
    }
}
