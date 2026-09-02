namespace StudentRoadMap.Domain.Schools;

/// <summary>
/// Maktab havolasining kunlik ochilish soni — dashboard voronkasining ENG YUQORI bo'g'ini
/// (`docs/07-api-shartnoma.md` 3.6-bo'lim kengaytmasi, `prompts/15` 2026-09-02 vazifasi:
/// "havola ochilishini hisoblash"). `RegistrationCounter` bilan BIR XIL naqsh — kompozit kalit
/// (`SchoolId`, `DateUtc`), `Entity`/`AggregateRoot` bazaviy sinflaridan meros olmaydi (`docs/05`
/// DDL: `PRIMARY KEY (school_id, date_utc)`, `Guid Id` ustuni yo'q).
///
/// **Shaxsiy ma'lumot EMAS** — faqat kunlik son, IP yoki qurilma saqlanmaydi (`prompts/15`
/// vazifa 1 "Diqqat"). **Cheklov:** takroriy sahifa ochish va botlar sonni oshiradi (MVP uchun
/// qabul qilingan — rate limit `RateLimitSetup.PublicSchoolInfo` orqali cheklangan).
///
/// Amalda poyga holatining oldini olish uchun hisoblagich atomik `INSERT ... ON CONFLICT
/// DO UPDATE` xom SQL orqali oshiriladi (`IAppDbContext.IncrementSchoolLinkViewAsync`,
/// `docs/08-auth-va-xavfsizlik.md` 3-bo'lim ruxsati — `RegistrationCounter` bilan bir xil sabab)
/// — shu sabab bu klassda mutatsiya metodi yo'q, faqat o'qish uchun.
/// </summary>
public sealed class SchoolLinkView
{
    public Guid SchoolId { get; private set; }

    public DateOnly DateUtc { get; private set; }

    public int Count { get; private set; }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private SchoolLinkView()
    {
    }

    private SchoolLinkView(Guid schoolId, DateOnly dateUtc, int count)
    {
        SchoolId = schoolId;
        DateUtc = dateUtc;
        Count = count;
    }

    /// <summary>Odatda faqat testlarda/qo'lda materiallashtirish uchun — production oqimi xom SQL upsert ishlatadi.</summary>
    public static SchoolLinkView Create(Guid schoolId, DateOnly dateUtc, int count = 1)
    {
        if (schoolId == Guid.Empty)
        {
            throw new ArgumentException("Maktab identifikatori bo'sh bo'lishi mumkin emas.", nameof(schoolId));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Hisoblagich manfiy bo'lishi mumkin emas.");
        }

        return new SchoolLinkView(schoolId, dateUtc, count);
    }
}
