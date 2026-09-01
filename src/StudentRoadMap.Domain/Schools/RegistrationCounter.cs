namespace StudentRoadMap.Domain.Schools;

/// <summary>
/// Maktabning kunlik ro'yxatdan o'tish hisoblagichi — BR-1/suiiste'molga qarshi chora
/// (`docs/08-auth-va-xavfsizlik.md` 3-bo'lim: "Maktab kunlik ro'yxatdan o'tish limiti").
/// Kompozit kalit (`SchoolId`, `DateUtc`) — `Entity`/`AggregateRoot` bazaviy sinflari yagona
/// `Guid Id` talab qiladi, shu sabab bu klass ulardan meros olmaydi (`docs/05` DDL:
/// `PRIMARY KEY (school_id, date_utc)`, `Guid` ustuni yo'q).
///
/// Amalda poyga holatining oldini olish uchun hisoblagich atomik `INSERT ... ON CONFLICT
/// DO UPDATE` xom SQL orqali oshiriladi (`IAppDbContext.IncrementRegistrationCounterAsync`,
/// `docs/08` 3-bo'lim ruxsati) — shu sabab bu klassda mutatsiya metodi yo'q, faqat o'qish uchun.
/// </summary>
public sealed class RegistrationCounter
{
    public Guid SchoolId { get; private set; }

    public DateOnly DateUtc { get; private set; }

    public int Count { get; private set; }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private RegistrationCounter()
    {
    }

    private RegistrationCounter(Guid schoolId, DateOnly dateUtc, int count)
    {
        SchoolId = schoolId;
        DateUtc = dateUtc;
        Count = count;
    }

    /// <summary>Odatda faqat testlarda/qo'lda materiallashtirish uchun — production oqimi xom SQL upsert ishlatadi.</summary>
    public static RegistrationCounter Create(Guid schoolId, DateOnly dateUtc, int count = 1)
    {
        if (schoolId == Guid.Empty)
        {
            throw new ArgumentException("Maktab identifikatori bo'sh bo'lishi mumkin emas.", nameof(schoolId));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Hisoblagich manfiy bo'lishi mumkin emas.");
        }

        return new RegistrationCounter(schoolId, dateUtc, count);
    }
}
