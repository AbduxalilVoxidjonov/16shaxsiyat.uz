using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Identity;

/// <summary>
/// Admin foydalanuvchi (MVP'da yagona superadmin). Blokirovka siyosati `docs/08-auth-va-xavfsizlik.md`
/// 28-qatorga mos: 5 ta noto'g'ri urinishdan keyin 15 daqiqaga bloklanadi.
/// </summary>
public sealed class AdminUser : Entity
{
    public const int MaxFailedLoginAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public string Username { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public string? FullName { get; private set; }

    public string PasswordHash { get; private set; } = null!;

    public AdminRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public string? TotpSecretEncrypted { get; private set; }

    public bool TotpEnabled { get; private set; }

    /// <summary>
    /// TOTP qayta ishlatishga qarshi himoya (`docs/13-auth-va-jwt.md` MAXSUS DIQQAT 5-band:
    /// "bir kod ikki marta ishlamasin") — oxirgi qabul qilingan RFC 6238 vaqt qadami (30 s
    /// oynalar soni, Unix vaqtidan). Yangi kod shu qiymatdan katta bo'lishi shart, aks holda
    /// (bir xil yoki eskiroq qadam) rad etiladi.
    /// </summary>
    public long? TotpLastUsedStep { get; private set; }

    public int FailedLoginCount { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Optimistik konkurentlik tokeni (QA topilmasi, `docs/13-auth-va-jwt.md`): har `Modified`
    /// saqlashda `AppDbContext.SaveChangesAsync` yangi qiymatga o'rnatadi — bir vaqtda ikki
    /// yozuv (masalan, TOTP asosiy kod poyga holati) bittasini `ConcurrencyConflictException`
    /// bilan rad etadi. Domendan tashqarida hech qachon o'qilmaydi/tekshirilmaydi.
    /// </summary>
    public Guid ConcurrencyStamp { get; private set; }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private AdminUser()
    {
    }

    private AdminUser(Guid id, string username, string email, string passwordHash, AdminRole role, string? fullName, DateTimeOffset now)
        : base(id)
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        FullName = fullName;
        IsActive = true;
        TotpEnabled = false;
        FailedLoginCount = 0;
        CreatedAt = now;
        UpdatedAt = now;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public static AdminUser Create(Guid id, string username, string email, string passwordHash, DateTimeOffset now, AdminRole role = AdminRole.SuperAdmin, string? fullName = null)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Foydalanuvchi nomi bo'sh bo'lishi mumkin emas.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email bo'sh bo'lishi mumkin emas.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Parol xeshi bo'sh bo'lishi mumkin emas.", nameof(passwordHash));
        }

        return new AdminUser(id, username, email, passwordHash, role, fullName, now);
    }

    /// <summary>Muvaffaqiyatsiz kirish urinishini qayd etadi — chegaraga yetganda bloklaydi.</summary>
    public void RegisterFailedLogin(DateTimeOffset now)
    {
        FailedLoginCount++;

        if (FailedLoginCount >= MaxFailedLoginAttempts)
        {
            LockedUntil = now.Add(LockoutDuration);
        }

        UpdatedAt = now;
    }

    /// <summary>Muvaffaqiyatli kirishda hisoblagichni tozalaydi.</summary>
    public void ResetFailedLogins(DateTimeOffset now)
    {
        FailedLoginCount = 0;
        LockedUntil = null;
        LastLoginAt = now;
        UpdatedAt = now;
    }

    public bool IsLocked(DateTimeOffset now) => LockedUntil.HasValue && LockedUntil.Value > now;

    public void ChangePasswordHash(string newPasswordHash, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new ArgumentException("Parol xeshi bo'sh bo'lishi mumkin emas.", nameof(newPasswordHash));
        }

        PasswordHash = newPasswordHash;
        UpdatedAt = now;
    }

    public void EnableTotp(string totpSecretEncrypted, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(totpSecretEncrypted))
        {
            throw new ArgumentException("TOTP siri bo'sh bo'lishi mumkin emas.", nameof(totpSecretEncrypted));
        }

        TotpSecretEncrypted = totpSecretEncrypted;
        TotpEnabled = true;
        UpdatedAt = now;
    }

    public void DisableTotp(DateTimeOffset now)
    {
        TotpSecretEncrypted = null;
        TotpEnabled = false;
        TotpLastUsedStep = null;
        UpdatedAt = now;
    }

    /// <summary>
    /// Muvaffaqiyatli TOTP tekshiruvidan keyin qabul qilingan vaqt qadamini qayd etadi —
    /// keyingi tekshiruvda shu qadam yoki undan eskisi qayta qabul qilinmaydi (replay himoyasi).
    /// </summary>
    public void RegisterTotpStepUsed(long step, DateTimeOffset now)
    {
        TotpLastUsedStep = step;
        UpdatedAt = now;
    }

    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAt = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }
}
