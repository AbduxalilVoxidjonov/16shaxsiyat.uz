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
/// <b>Standart qiymatlar</b> (<see cref="Default"/>) — `birthDate`/`grade`/`phone`/`gender`
/// majburiy, qolgani ixtiyoriy (`gender` 2026-09-11 kuni `Optional`dan `Required`ga
/// ko'chirildi — ommaviy ro'yxat formasi (`registrationSchema.ts`) jinsni ALLAQACHON
/// majburiy qilardi, backend esa buni tekshirmasdi; ikki tomonni bitta xatti-harakatga
/// keltirish uchun backend frontendga moslashtirildi, aksincha emas). Dastur
/// `RegistrationFields = null` (bazada `NULL`) bo'lsa shu qiymatlar ishlatiladi
/// (<see cref="AssessmentProgram.ResolveRegistrationFields"/>).
/// </para>
///
/// <para>
/// <b>Diqqat — ikki tomonlama shartnoma:</b> standart qiymatlar
/// `frontend/src/shared/api/registrationModeTypes.ts` dagi `DEFAULT_REGISTRATION_FIELDS`
/// bilan BIR XIL bo'lishi SHART — biri o'zgarsa ikkinchisi ham shu zahoti yangilanishi kerak
/// (aks holda forma va server bir-biriga zid talab qo'yadi).
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
    /// `docs/18` §9.5 jadvali — `frontend`dagi `DEFAULT_REGISTRATION_FIELDS` bilan AYNAN mos
    /// bo'lishi shart (yuqoridagi izohga qarang).
    /// </summary>
    public static readonly RegistrationFields Default = new(
        BirthDate: RegistrationFieldRequirement.Required,
        Gender: RegistrationFieldRequirement.Required,
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
