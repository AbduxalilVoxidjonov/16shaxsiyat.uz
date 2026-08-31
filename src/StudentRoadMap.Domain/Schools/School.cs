using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Events;

namespace StudentRoadMap.Domain.Schools;

/// <summary>
/// Maktab — agregat ildizi. Ommaviy havola (`Slug` + `AccessToken`) orqali o'quvchilar
/// ro'yxatdan o'tadi (`docs/04` 2.1-bo'lim).
/// </summary>
public sealed class School : AggregateRoot
{
    public string Name { get; private set; } = null!;

    public string Region { get; private set; } = null!;

    public string District { get; private set; } = null!;

    public string? SchoolNumber { get; private set; }

    public string? ContactPerson { get; private set; }

    public string? ContactPhone { get; private set; }

    public SchoolSlug Slug { get; private set; } = null!;

    public string AccessToken { get; private set; } = null!;

    public string? AccessCode { get; private set; }

    public int DailyRegistrationLimit { get; private set; }

    public bool IsActive { get; private set; }

    public string? Notes { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private School()
    {
    }

    private School(
        Guid id,
        string name,
        string region,
        string district,
        string? schoolNumber,
        string? contactPerson,
        string? contactPhone,
        SchoolSlug slug,
        string accessToken,
        string? accessCode,
        int dailyRegistrationLimit,
        string? notes,
        DateTimeOffset now)
        : base(id)
    {
        Name = name;
        Region = region;
        District = district;
        SchoolNumber = schoolNumber;
        ContactPerson = contactPerson;
        ContactPhone = contactPhone;
        Slug = slug;
        AccessToken = accessToken;
        AccessCode = accessCode;
        DailyRegistrationLimit = dailyRegistrationLimit;
        Notes = notes;
        IsActive = true;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static School Create(
        Guid id,
        string name,
        string region,
        string district,
        SchoolSlug slug,
        string accessToken,
        DateTimeOffset now,
        string? schoolNumber = null,
        string? contactPerson = null,
        string? contactPhone = null,
        string? accessCode = null,
        int dailyRegistrationLimit = 500,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Maktab nomi bo'sh bo'lishi mumkin emas.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(region))
        {
            throw new ArgumentException("Viloyat bo'sh bo'lishi mumkin emas.", nameof(region));
        }

        if (string.IsNullOrWhiteSpace(district))
        {
            throw new ArgumentException("Tuman/shahar bo'sh bo'lishi mumkin emas.", nameof(district));
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException("Havola tokeni bo'sh bo'lishi mumkin emas.", nameof(accessToken));
        }

        if (dailyRegistrationLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dailyRegistrationLimit), "Kunlik ro'yxatdan o'tish limiti musbat bo'lishi kerak.");
        }

        return new School(
            id,
            name,
            region,
            district,
            schoolNumber,
            contactPerson,
            contactPhone,
            slug,
            accessToken,
            accessCode,
            dailyRegistrationLimit,
            notes,
            now);
    }

    /// <summary>Havolani qayta generatsiya qiladi — eski token darhol yaroqsiz bo'ladi.</summary>
    public void RegenerateAccessToken(string newAccessToken, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(newAccessToken))
        {
            throw new ArgumentException("Yangi token bo'sh bo'lishi mumkin emas.", nameof(newAccessToken));
        }

        AccessToken = newAccessToken;
        UpdatedAt = now;

        RaiseDomainEvent(new SchoolLinkRegeneratedEvent(Id, now));
    }

    /// <summary>Maktabni faolsizlantiradi — ommaviy API `410 Gone` qaytaradi.</summary>
    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    /// <summary>Maktabni qayta faollashtiradi.</summary>
    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAt = now;
    }

    /// <summary>Ma'muriy ma'lumotlarni yangilaydi (Slug va AccessToken bu yerda o'zgarmaydi).</summary>
    public void UpdateDetails(
        string name,
        string region,
        string district,
        string? schoolNumber,
        string? contactPerson,
        string? contactPhone,
        string? accessCode,
        int dailyRegistrationLimit,
        string? notes,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Maktab nomi bo'sh bo'lishi mumkin emas.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(region))
        {
            throw new ArgumentException("Viloyat bo'sh bo'lishi mumkin emas.", nameof(region));
        }

        if (string.IsNullOrWhiteSpace(district))
        {
            throw new ArgumentException("Tuman/shahar bo'sh bo'lishi mumkin emas.", nameof(district));
        }

        if (dailyRegistrationLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dailyRegistrationLimit), "Kunlik ro'yxatdan o'tish limiti musbat bo'lishi kerak.");
        }

        Name = name;
        Region = region;
        District = district;
        SchoolNumber = schoolNumber;
        ContactPerson = contactPerson;
        ContactPhone = contactPhone;
        AccessCode = accessCode;
        DailyRegistrationLimit = dailyRegistrationLimit;
        Notes = notes;
        UpdatedAt = now;
    }

    /// <summary>
    /// Yumshoq o'chirish (`docs/05` DDL: `schools.is_deleted` + `deleted_at`; `deleted_by`
    /// ustuni jadvalda yo'q — `docs/04` 5-bo'limdagi `DeletedByAdminUserId` shu sabab olib
    /// tashlandi, audit "kim o'chirdi"ni `AuditLog` orqali kuzatadi).
    /// </summary>
    public void MarkDeleted(DateTimeOffset now)
    {
        IsDeleted = true;
        DeletedAt = now;
        UpdatedAt = now;
    }

    public void Restore(DateTimeOffset now)
    {
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = now;
    }
}
