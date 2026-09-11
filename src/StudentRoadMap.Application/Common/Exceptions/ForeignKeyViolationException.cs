namespace StudentRoadMap.Application.Common.Exceptions;

/// <summary>
/// DB darajasidagi tashqi kalit (FK) cheklovi buzilganda otiladi — `UniqueConstraintViolationException`
/// bilan BIR XIL naqsh (`docs/06` 3-bo'lim: `Application` EF Core paketiga bog'lanmaydi, shu sabab
/// `Infrastructure`dagi `AppDbContext.SaveChangesAsync` EF Core'ning umumiy `DbUpdateException`sini
/// shu portativ istisnoga aylantiradi).
///
/// **Qachon ishlatiladi (P52, 2026-09-11 QA topilmasi):** egasi javobga bog'liq savolni
/// o'chirishga urindi — `fk_answers_questions_question_id` (`23503`) buzildi va bu jimgina
/// `500 INTERNAL_ERROR` bo'lib chiqdi. Aniq holatlar (savolga javob berilgan, savolga
/// `visibility` shart tayanadi) endi OLDINDAN tekshiriladi va aniq xato kodi bilan rad etiladi
/// (`QUESTION_IN_USE`/`QUESTION_REFERENCED_BY_VISIBILITY`) — bu istisno faqat ULARNI CHETLAB
/// o'tgan holatlar (poyga, boshqa endpointlar) uchun ZAXIRA: foydalanuvchi hech qachon `500`
/// ko'rmasin. Xabar DOIM o'zgarmas umumiy matn — DB jadval/cheklov nomi HECH QACHON chiqmaydi (P31).
/// </summary>
public sealed class ForeignKeyViolationException : Exception
{
    public string Code { get; }

    public ForeignKeyViolationException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public ForeignKeyViolationException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}
