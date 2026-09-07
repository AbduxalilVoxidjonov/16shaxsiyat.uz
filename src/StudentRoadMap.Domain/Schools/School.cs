using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Events;

namespace StudentRoadMap.Domain.Schools;

/// <summary>
/// Maktab — agregat ildizi. Ommaviy havola (`Slug` + `AccessToken`) orqali o'quvchilar
/// ro'yxatdan o'tadi (`docs/04` 2.1-bo'lim).
/// </summary>
public sealed class School : AggregateRoot
{
    /// <summary>Ommaviy makonning kunlik ro'yxatdan o'tish limiti — maktabnikidan ancha katta.</summary>
    public const int DefaultPublicSpaceRegistrationLimit = 100_000;

    public string Name { get; private set; } = null!;

    public string Region { get; private set; } = null!;

    public string District { get; private set; } = null!;

    public string? SchoolNumber { get; private set; }

    public string? ContactPerson { get; private set; }

    public string? ContactPhone { get; private set; }

    public SchoolSlug Slug { get; private set; } = null!;

    /// <summary>
    /// Makon turi (`SchoolKind`). `School` — klassik maktab havolasi oqimi; `PublicSpace` —
    /// Telegram orqali kirgan tashqi foydalanuvchilar makoni (bazada AYNAN BITTA).
    /// </summary>
    public SchoolKind Kind { get; private set; }

    public string AccessToken { get; private set; } = null!;

    public string? AccessCode { get; private set; }

    /// <summary>
    /// **Maktab kodi** (`SchoolEntryCode`) — `/kirish` sahifasidagi "Maktab uchun" yo'li uchun
    /// 8 belgili sir; `POST /api/public/schools/resolve-code` uni `{ slug, accessToken }` ga
    /// aylantiradi. `Kind == School` uchun HAR DOIM to'ldirilgan (yaratishda beriladi),
    /// `PublicSpace` uchun HAR DOIM `null` — ommaviy makonga kod bilan kirish yo'q, u faqat
    /// Telegram orqali. Bazada `ux_schools_entry_code` qisman unikal indeksi (`entry_code IS
    /// NOT NULL`). Saqlash shakli defissiz (`ABCD2345`), ko'rsatishda `ABCD-2345`.
    ///
    /// `AccessCode` (ixtiyoriy 6 raqamli "sinf kodi", anketadagi qo'shimcha tekshiruv) bilan
    /// ALOQASI YO'Q — ikkalasi yonma-yon yashaydi.
    /// </summary>
    public string? EntryCode { get; private set; }

    public int DailyRegistrationLimit { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// O'quvchiga test natijasi ko'rsatiladimi. Ilgari bu GLOBAL sozlama edi
    /// (`App:ShowResultToStudent`, standart `false`) — endi HAR MAKON O'ZI hal qiladi:
    /// maktab natijani odatda psixolog orqali beradi (`false`), ommaviy makonda esa
    /// foydalanuvchi o'z natijasini ko'rmasa mahsulotning ma'nosi yo'q (`true`).
    /// Global sozlama BEKOR QILINMADI — u "avariya rubilnigi" (kill-switch) sifatida qoladi:
    /// Application qatlami ikkalasini VA (`global && school`) bilan birlashtiradi, ya'ni
    /// global `false` butun tizim bo'ylab yopib qo'yadi (huquqiy/insident holati uchun),
    /// global `true` esa qarorni makonga topshiradi.
    /// </summary>
    public bool ShowResultToStudent { get; private set; }

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
        string? entryCode,
        int dailyRegistrationLimit,
        string? notes,
        SchoolKind kind,
        bool showResultToStudent,
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
        EntryCode = entryCode;
        DailyRegistrationLimit = dailyRegistrationLimit;
        Notes = notes;
        Kind = kind;
        ShowResultToStudent = showResultToStudent;
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
        string entryCode,
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

        EnsureValidEntryCode(entryCode, nameof(entryCode));

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
            entryCode,
            dailyRegistrationLimit,
            notes,
            SchoolKind.School,
            showResultToStudent: false,
            now);
    }

    /// <summary>
    /// YAGONA ommaviy makonni yaratadi (`SchoolKind.PublicSpace`). Faqat seeder chaqiradi
    /// (`DbSeeder.SeedPublicSpaceAsync`, idempotent, barqaror slug `ommaviy`) — admin API
    /// orqali ikkinchisini yaratib bo'lmaydi, chunki `Create` har doim `SchoolKind.School`
    /// qaytaradi va DB'da `ux_schools_public_space` qisman unikal indeksi turadi.
    ///
    /// `ShowResultToStudent = true` — tashqi foydalanuvchi o'z natijasini ko'rmasa
    /// shaxsiy kabinetning ma'nosi qolmaydi.
    /// </summary>
    public static School CreatePublicSpace(
        Guid id,
        string name,
        SchoolSlug slug,
        string accessToken,
        DateTimeOffset now,
        int dailyRegistrationLimit = DefaultPublicSpaceRegistrationLimit,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Makon nomi bo'sh bo'lishi mumkin emas.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException("Havola tokeni bo'sh bo'lishi mumkin emas.", nameof(accessToken));
        }

        if (dailyRegistrationLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dailyRegistrationLimit), "Kunlik ro'yxatdan o'tish limiti musbat bo'lishi kerak.");
        }

        // `Region`/`District` maktab uchun majburiy ustunlar — ommaviy makonda geografiya
        // ma'nosiz, shu sabab barqaror "Ommaviy" qiymati yoziladi (ustunni nullable qilish
        // 5 ta EF konfiguratsiya + admin filtrlarini o'zgartirishni talab qilardi).
        return new School(
            id,
            name,
            region: "Ommaviy",
            district: "Ommaviy",
            schoolNumber: null,
            contactPerson: null,
            contactPhone: null,
            slug,
            accessToken,
            accessCode: null,
            // Ommaviy makonga kod bilan kirish YO'Q (faqat Telegram) — `docs/08` 3a-bo'lim.
            entryCode: null,
            dailyRegistrationLimit,
            notes,
            SchoolKind.PublicSpace,
            showResultToStudent: true,
            now);
    }

    /// <summary>Ommaviy makonmi — `Kind == SchoolKind.PublicSpace` uchun qisqartma.</summary>
    public bool IsPublicSpace => Kind == SchoolKind.PublicSpace;

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

    /// <summary>
    /// Maktab kodini qayta generatsiya qiladi — eski kod DARHOL ishlamay qoladi
    /// (`RegenerateAccessToken` bilan bir xil naqsh; kod ham havola kabi tarqatiladigan sir).
    /// Ommaviy makonga qo'llanmaydi — unda kod umuman yo'q.
    /// </summary>
    public void RegenerateEntryCode(string newEntryCode, DateTimeOffset now)
    {
        EnsureNotPublicSpace("Ommaviy makonda maktab kodi yo'q.");
        EnsureValidEntryCode(newEntryCode, nameof(newEntryCode));

        EntryCode = newEntryCode;
        UpdatedAt = now;

        RaiseDomainEvent(new SchoolEntryCodeRegeneratedEvent(Id, now));
    }

    /// <summary>
    /// Maktabni faolsizlantiradi — ommaviy API `410 Gone` qaytaradi. Ommaviy makonga
    /// qo'llanmaydi: uni o'chirish butun ommaviy oqimni (va tashqi foydalanuvchilarning
    /// kabinetini) jimgina o'ldirardi.
    /// </summary>
    public void Deactivate(DateTimeOffset now)
    {
        EnsureNotPublicSpace("Ommaviy makonni faolsizlantirib bo'lmaydi.");

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
        EnsureNotPublicSpace("Ommaviy makonni o'chirib bo'lmaydi.");

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

    /// <summary>
    /// Natijani o'quvchiga ko'rsatish bayrog'ini o'zgartiradi. `UpdateDetails` ga qo'shilmadi —
    /// uning imzosi admin `UpdateSchool` oqimida ishlatiladi va o'zgarishi keyingi qatlamni
    /// (handler/kontrakt) buzardi; bu bayroq esa mustaqil, alohida boshqariladigan sozlama.
    /// </summary>
    public void SetShowResultToStudent(bool showResultToStudent, DateTimeOffset now)
    {
        ShowResultToStudent = showResultToStudent;
        UpdatedAt = now;
    }

    private static void EnsureValidEntryCode(string entryCode, string paramName)
    {
        if (!SchoolEntryCode.IsValid(entryCode))
        {
            throw new ArgumentException(
                $"Maktab kodi {SchoolEntryCode.Length} belgidan, faqat '{SchoolEntryCode.Alphabet}' alifbosidan bo'lishi kerak.",
                paramName);
        }
    }

    private void EnsureNotPublicSpace(string message)
    {
        if (Kind == SchoolKind.PublicSpace)
        {
            throw new DomainException("SCHOOL_PUBLIC_SPACE_PROTECTED", message);
        }
    }
}
