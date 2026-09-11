namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// `AssessmentProgram.RegistrationFields` — qiymat obyekti (P52 kengaytmasi, `docs/18` §9.5,
/// egasining qarori): `RegistrationMode.Full` dasturda ro'yxatdan o'tish anketasining QAYSI
/// maydonlari ko'rsatilishini, ixtiyoriy yoki majburiy ekanini HAR DASTURDA alohida belgilaydi.
///
/// <para>
/// <b>`FullName` bu yerda YO'Q</b> — `RegistrationMode = Full` bo'lsa F.I.Sh. HAR DOIM majburiy
/// (`StartSessionCommandHandler.ValidateRequiredIdentityFields`). Ism kerak bo'lmasa
/// `RegistrationMode = None` (anonim oqim) bor — ikkita mustaqil "ism shart emas" mexanizmi
/// chalkashlik keltirib chiqarardi.
/// </para>
///
/// <para>
/// <b>Standart qiymatlar</b> (<see cref="Default"/>) — mavjud (2026-09-11 gacha) xatti-harakat
/// bilan AYNAN mos: `birthDate`/`grade`/`phone` majburiy, qolgani ixtiyoriy. Dastur
/// `RegistrationFields = null` (bazada `NULL`) bo'lsa shu qiymatlar ishlatiladi
/// (<see cref="AssessmentProgram.ResolveRegistrationFields"/>).
/// </para>
/// </summary>
public sealed record RegistrationFields(
    RegistrationFieldRequirement BirthDate,
    RegistrationFieldRequirement Gender,
    RegistrationFieldRequirement Grade,
    RegistrationFieldRequirement ClassLetter,
    RegistrationFieldRequirement Phone,
    RegistrationFieldRequirement ParentPhone,
    RegistrationFieldRequirement Email)
{
    /// <summary>
    /// Mavjud xatti-harakat bilan bayt-bayt mos standart to'plam — `docs/18` §9.5 jadvali.
    /// </summary>
    public static readonly RegistrationFields Default = new(
        BirthDate: RegistrationFieldRequirement.Required,
        Gender: RegistrationFieldRequirement.Optional,
        Grade: RegistrationFieldRequirement.Required,
        ClassLetter: RegistrationFieldRequirement.Optional,
        Phone: RegistrationFieldRequirement.Required,
        ParentPhone: RegistrationFieldRequirement.Optional,
        Email: RegistrationFieldRequirement.Optional);

    /// <summary>
    /// Shaxsiyat batareyasi invarianti (`docs/18` §9.5): scoring/normalar/AI tahlili yosh va
    /// sinfga tayanadi — batareya bor dasturda `BirthDate`/`Grade` DOIM `Required` bo'lishi
    /// shart. `Gender` ATAYLAB bu yerda YO'Q — u hech qachon majburiy qilinmagan (mavjud
    /// xatti-harakat buzilmasin).
    /// </summary>
    public bool SatisfiesPersonalityBatteryInvariant() =>
        BirthDate == RegistrationFieldRequirement.Required && Grade == RegistrationFieldRequirement.Required;
}
