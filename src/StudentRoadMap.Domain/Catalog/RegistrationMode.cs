namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// `AssessmentProgram.RegistrationMode` — P52 (`docs/18-tarmoqlanuvchi-sorovnoma.md`,
/// egasining 2026-09-11 qarori): ro'yxatdan o'tish (F.I.Sh., tug'ilgan sana, jins, sinf,
/// telefon) endi DASTURGA biriktiriladi, so'rovnomaning o'z savollariga emas.
///
/// <para>
/// <b>Qat'iy invariant</b> (`AssessmentProgram.Publish`/`SetRegistrationMode`,
/// `Domain.Catalog.PersonalityBattery`): dasturda ilmiy shaxsiyat batareyasi (`Standard` +
/// `Scored` metodika) bo'lsa `RegistrationMode` DOIM `Full` bo'lishi shart — scoring, normalar
/// va AI tahlili yosh/sinf/jinsga tayanadi, ularsiz natija ma'nosiz bo'ladi. Buzilsa
/// `DomainException("REGISTRATION_REQUIRED_FOR_BATTERY")`.
/// </para>
/// </summary>
public enum RegistrationMode
{
    /// <summary>Standart: o'quvchi ro'yxatdan o'tish anketasini to'ldiradi (F.I.Sh., tug'ilgan sana, jins, sinf, telefon).</summary>
    Full = 1,

    /// <summary>
    /// Registratsiya ekrani UMUMAN ko'rsatilmaydi — `Student` ANONIM yaratiladi
    /// (`Student.CreateAnonymous`). Shaxs ma'lumoti (agar kerak bo'lsa) so'rovnomaning o'z
    /// javoblarida qoladi. Faqat shaxsiyat batareyasi YO'Q dasturlarda ruxsat etiladi.
    /// </summary>
    None = 2,
}
