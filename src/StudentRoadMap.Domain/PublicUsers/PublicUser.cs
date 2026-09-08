using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.PublicUsers;

/// <summary>
/// Ommaviy (tashqi) foydalanuvchi — Telegram orqali kiradigan shaxs, agregat ildizi
/// (`docs/04` 2.11-bo'lim). Maktab oqimidagi o'quvchidan MUTLAQO farq qiladi: maktabda
/// ro'yxatdan o'tish yo'q (havola + F.I.Sh. yetarli), bu yerda esa akkaunt bor — shaxsiy
/// kabinet, test tarixi, natijalar shu akkauntga bog'lanadi.
///
/// Superadmin `AdminUser`i bilan ARALASHTIRILMAYDI: alohida jadval, alohida refresh token
/// jadvali (<see cref="PublicRefreshToken"/>) va (keyingi qatlamda) alohida JWT rol/policy.
/// </summary>
public sealed class PublicUser : AggregateRoot
{
    public const int MaxUsernameLength = 64;
    public const int MaxNameLength = 100;
    public const int MaxPhotoUrlLength = 500;
    public const int MaxDeletionCommentLength = 500;

    /// <summary>
    /// Telegram foydalanuvchi identifikatori (`bigint`, unikal). Telegram ID'lari 2^53 dan
    /// oshib ketishi mumkinligi rasman e'lon qilingan — shu sabab `int` emas, `long`.
    ///
    /// Nima uchun NULLABLE bo'lishi mumkin: foydalanuvchi "ma'lumotimni o'chiring" deganda
    /// yozuv butunlay o'chirilmaydi (test tarixi `docs/08` bo'yicha 5 yil saqlanadi va
    /// `students.public_user_id` FK'si buziladi), balki ANONIMLASHTIRILADI — `MarkDeleted`
    /// Telegram ID va profil maydonlarini tozalaydi. Ya'ni shaxsni aniqlovchi barcha
    /// ma'lumot yo'qoladi, statistika esa saqlanadi. Yaratilishda esa ID HAR DOIM majburiy.
    /// </summary>
    public long? TelegramId { get; private set; }

    public string? Username { get; private set; }

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public string? PhotoUrl { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Oxirgi muvaffaqiyatli kirish vaqti — yaratilishda ham to'ldiriladi (kirish = ro'yxatdan o'tish).</summary>
    public DateTimeOffset LastLoginAt { get; private set; }

    /// <summary>
    /// Yumshoq o'chirish belgisi. `School`/`Student` dagi `IsDeleted` + `DeletedAt` juftligi
    /// EMAS, faqat `DeletedAt` — chunki bu yerda "o'chirilgan" holat ANONIMLASHTIRISH bilan
    /// birga keladi va ikkita ustunni sinxron ushlab turishning hojati yo'q
    /// (`DeletedAt is not null` yagona haqiqat manbai).
    /// </summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>
    /// Foydalanuvchi tanlagan o'chirish sababi (egasining 2026-09-08 qarori — o'chirishdan
    /// oldin sabab so'raladi). Anonimlashtirish PROFIL maydonlarini tozalaydi, lekin BU
    /// maydon ATAYLAB saqlanadi — superadmin ro'yxatida "nega o'chirilgan" ko'rinishi kerak
    /// (aks holda `MarkDeleted` boshqa maydonlar kabi buni ham `null` qilib qo'yardi).
    /// </summary>
    public PublicUserDeletionReason? DeletionReason { get; private set; }

    /// <summary>Ixtiyoriy erkin matnli izoh (≤500 belgi); `Other` sababida MAJBURIY (validatorda).</summary>
    public string? DeletionComment { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private PublicUser()
    {
    }

    private PublicUser(
        Guid id,
        long telegramId,
        string? username,
        string? firstName,
        string? lastName,
        string? photoUrl,
        DateTimeOffset now)
        : base(id)
    {
        TelegramId = telegramId;
        Username = username;
        FirstName = firstName;
        LastName = lastName;
        PhotoUrl = photoUrl;
        CreatedAt = now;
        UpdatedAt = now;
        LastLoginAt = now;
    }

    /// <summary>
    /// Yangi ommaviy foydalanuvchi. Telegram profil maydonlarining barchasi ixtiyoriy —
    /// Telegram `username`/`last_name`/`photo_url` ni har doim bermaydi.
    /// </summary>
    public static PublicUser Create(
        Guid id,
        long telegramId,
        DateTimeOffset now,
        string? username = null,
        string? firstName = null,
        string? lastName = null,
        string? photoUrl = null)
    {
        if (telegramId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(telegramId), "Telegram identifikatori musbat bo'lishi kerak.");
        }

        return new PublicUser(
            id,
            telegramId,
            Trim(username, MaxUsernameLength),
            Trim(firstName, MaxNameLength),
            Trim(lastName, MaxNameLength),
            Trim(photoUrl, MaxPhotoUrlLength),
            now);
    }

    /// <summary>
    /// Har kirishda Telegram'dan kelgan profil ma'lumotini yangilaydi (ism o'zgargan,
    /// avatar almashgan bo'lishi mumkin) va `LastLoginAt` ni belgilaydi.
    /// </summary>
    public void RecordLogin(DateTimeOffset now, string? username = null, string? firstName = null, string? lastName = null, string? photoUrl = null)
    {
        EnsureNotDeleted();

        Username = Trim(username, MaxUsernameLength);
        FirstName = Trim(firstName, MaxNameLength);
        LastName = Trim(lastName, MaxNameLength);
        PhotoUrl = Trim(photoUrl, MaxPhotoUrlLength);
        LastLoginAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// "Ma'lumotimni o'chiring" (self-service) — yozuv qoladi, lekin shaxsni aniqlovchi
    /// BARCHA maydon tozalanadi. Qattiq o'chirish tanlanmadi, chunki:
    /// (1) `students.public_user_id` FK'si va test tarixi (`docs/08`: natijalar 5 yil)
    ///     buzilardi; (2) statistika/audit yaxlitligi yo'qolardi; (3) Telegram ID tozalangani
    ///     uchun bir xil foydalanuvchi qayta kirsa YANGI akkaunt oladi — bu aynan kutilgan
    ///     xatti-harakat.
    ///
    /// <paramref name="reason"/>/<paramref name="comment"/> — anonimlashtirish MANTIG'IGA
    /// TEGMAYDI (Telegram ID/ism/username/foto baribir tozalanadi), lekin O'ZI saqlanadi:
    /// egasining 2026-09-08 qarori bo'yicha superadmin "nega o'chirilgan"ni ko'ra olishi kerak.
    /// Idempotent: takroriy chaqiruvda sabab/izoh QAYTA YOZILMAYDI (birinchi o'chirish
    /// paytidagi qiymat — haqiqat manbai; takroriy chaqiruv odatda tarmoq qayta urinishi,
    /// yangi "sabab"ni qabul qilish ma'nosiz).
    /// </summary>
    public void MarkDeleted(DateTimeOffset now, PublicUserDeletionReason reason, string? comment = null)
    {
        if (IsDeleted)
        {
            return;
        }

        TelegramId = null;
        Username = null;
        FirstName = null;
        LastName = null;
        PhotoUrl = null;
        DeletedAt = now;
        DeletionReason = reason;
        DeletionComment = Trim(comment, MaxDeletionCommentLength);
        UpdatedAt = now;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new DomainException("PUBLIC_USER_DELETED", "Bu akkaunt o'chirilgan.");
        }
    }

    private static string? Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
