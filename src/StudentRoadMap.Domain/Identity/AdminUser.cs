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

    /// <summary>
    /// Tasdiqlanmagan TOTP o'rnatish (enrollment) sirining amal qilish muddati. Foydalanuvchi
    /// `POST /api/auth/totp/enable` dan keyin shu vaqt ichida autentifikator ilovasidagi kodni
    /// `POST /api/auth/totp/confirm` ga yuborishi kerak — aks holda kutish holatidagi sir
    /// yaroqsiz bo'ladi va jarayon boshidan boshlanadi (yarim qolgan o'rnatish DB'da abadiy
    /// osilib qolmasin).
    /// </summary>
    public static readonly TimeSpan PendingTotpEnrollmentLifetime = TimeSpan.FromMinutes(10);

    public string Username { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public string? FullName { get; private set; }

    public string PasswordHash { get; private set; } = null!;

    public AdminRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public string? TotpSecretEncrypted { get; private set; }

    public bool TotpEnabled { get; private set; }

    /// <summary>
    /// Tasdiqlanmagan (kutish holatidagi) TOTP siri — shifrlangan. `TotpSecretEncrypted` dan
    /// ATAYIN ajratilgan: foydalanuvchi autentifikator ilovasi to'g'ri kod berayotganini
    /// isbotlamaguncha 2FA YOQILMAYDI (`TotpEnabled` `false` qoladi), aks holda noto'g'ri
    /// o'rnatishdan keyin hisob keyingi kirishda butunlay bloklanib qolardi.
    /// </summary>
    public string? PendingTotpSecretEncrypted { get; private set; }

    /// <summary>Kutish holatidagi sir qachon yaratilgani — muddat (<see cref="PendingTotpEnrollmentLifetime"/>) shundan hisoblanadi.</summary>
    public DateTimeOffset? PendingTotpCreatedAt { get; private set; }

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

    /// <summary>
    /// TOTP o'rnatishning BIRINCHI bosqichi: yangi sirni KUTISH holatida saqlaydi. 2FA hali
    /// yoqilmaydi — `TotpEnabled` `false` qoladi va login oqimi o'zgarmaydi. Takroriy chaqiruv
    /// oldingi tasdiqlanmagan sirni almashtiradi (foydalanuvchi QR'ni qayta so'raganda).
    /// </summary>
    public void BeginTotpEnrollment(string totpSecretEncrypted, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(totpSecretEncrypted))
        {
            throw new ArgumentException("TOTP siri bo'sh bo'lishi mumkin emas.", nameof(totpSecretEncrypted));
        }

        if (TotpEnabled)
        {
            throw new DomainException("TOTP_ALREADY_ENABLED", "TOTP allaqachon yoqilgan.");
        }

        PendingTotpSecretEncrypted = totpSecretEncrypted;
        PendingTotpCreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>Kutish holatidagi sir mavjud va muddati o'tmaganmi.</summary>
    public bool HasValidPendingTotpEnrollment(DateTimeOffset now) =>
        PendingTotpSecretEncrypted is not null &&
        PendingTotpCreatedAt is not null &&
        now < PendingTotpCreatedAt.Value.Add(PendingTotpEnrollmentLifetime);

    /// <summary>Kutish holatidagi sir bor, lekin muddati o'tgan (mijozga aniq xato ko'rsatish uchun).</summary>
    public bool HasExpiredPendingTotpEnrollment(DateTimeOffset now) =>
        PendingTotpSecretEncrypted is not null && !HasValidPendingTotpEnrollment(now);

    /// <summary>
    /// TOTP o'rnatishning IKKINCHI bosqichi: kutish holatidagi sir asosiy sirga ko'chiriladi
    /// va 2FA YOQILADI. Chaqiruvchi buni faqat ilovadan kelgan kod tekshiruvdan o'tgandan
    /// KEYIN chaqiradi (`ITotpService.TryValidate`) — domen kod tekshirmaydi (`CLAUDE.md`
    /// 2-qoidasi: kriptografiya `Infrastructure`da).
    /// </summary>
    public void ConfirmTotpEnrollment(DateTimeOffset now)
    {
        if (TotpEnabled)
        {
            throw new DomainException("TOTP_ALREADY_ENABLED", "TOTP allaqachon yoqilgan.");
        }

        if (PendingTotpSecretEncrypted is null)
        {
            throw new DomainException("TOTP_ENROLLMENT_NOT_STARTED", "TOTP o'rnatish boshlanmagan.");
        }

        if (!HasValidPendingTotpEnrollment(now))
        {
            throw new DomainException("TOTP_ENROLLMENT_EXPIRED", "TOTP o'rnatish muddati tugagan.");
        }

        TotpSecretEncrypted = PendingTotpSecretEncrypted;
        TotpEnabled = true;
        TotpLastUsedStep = null;
        PendingTotpSecretEncrypted = null;
        PendingTotpCreatedAt = null;
        UpdatedAt = now;
    }

    /// <summary>Tasdiqlanmagan o'rnatishni bekor qiladi (kutish holatidagi sirni tozalaydi).</summary>
    public void CancelTotpEnrollment(DateTimeOffset now)
    {
        PendingTotpSecretEncrypted = null;
        PendingTotpCreatedAt = null;
        UpdatedAt = now;
    }

    public void DisableTotp(DateTimeOffset now)
    {
        TotpSecretEncrypted = null;
        TotpEnabled = false;
        TotpLastUsedStep = null;
        PendingTotpSecretEncrypted = null;
        PendingTotpCreatedAt = null;
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
