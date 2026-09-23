using StudentRoadMap.Application.Admin.Settings.RegistrationForm;
using StudentRoadMap.Application.Common.Models;

namespace StudentRoadMap.Application.Public.GetSchoolInfo;

/// <summary>
/// `docs/07-api-shartnoma.md` 1.1-bo'lim javob shakli. `Programs` — `prompts/34` C8-band
/// bilan qo'shildi: maktab uchun mavjud dasturlar (`Visibility = Public` yoki biriktirilgan).
///
/// **`Tests` (P52, 2026-09-11 jonli hodisadan keyin tuzatildi):** endi BUTUN katalogdan EMAS,
/// FAQAT `Programs` ichidagi MAVJUD dasturlar asosida hisoblanadi — `ProgramTestCatalog` bir xil
/// manbasi bilan (`AssessmentTestAttacher`/sessiya bilan bir xil ro'yxat). Bir nechta dastur
/// bo'lsa — birlashma (union), bitta test bir nechta dasturda bo'lsa ham TAKRORLANMAYDI. Ilgari
/// bu maydon "orqaga moslik uchun, dasturdan qat'i nazar butun katalog" edi — bu XATO edi:
/// dastur arxivlansa ham uning testlari bu yerda ko'rinishda davom etardi (jonli hodisa,
/// `docs/07` 1.1-bo'lim izohiga qarang).
/// </summary>
public sealed record GetSchoolInfoResult(
    Guid SchoolId,
    string Name,
    string Region,
    string District,
    bool RequiresAccessCode,
    IReadOnlyList<PublicTestCatalogItemDto> Tests,
    int TotalEstimatedMinutes,
    string ConsentText,
    IReadOnlyList<PublicProgramSummaryDto> Programs);

/// <summary>
/// Boshlanish ekranidagi bitta test bloki haqida ma'lumot (savol soni, taxminiy vaqt).
/// `Description` — katalogdagi `TestDefinition.DescriptionUz` (superadmin anketa
/// sozlamalarida to'ldiradi); bo'sh bo'lsa <c>null</c>. Frontend kartada shuni ustun
/// qo'yadi, i18n matni faqat zaxira (`docs/07` 1.1).
/// </summary>
public sealed record PublicTestCatalogItemDto(
    string Code,
    string Name,
    string? Description,
    int QuestionCount,
    int EstimatedMinutes,
    int Order);

/// <summary>
/// Maktab uchun mavjud bitta dastur — `docs/06` 8-bo'lim, `prompts/34` C8-band. O'quvchi kirishda
/// (bir nechtasi bo'lsa) shulardan bittasini tanlaydi (`code` — `POST /sessions` `programCode`ga).
///
/// `HasPersonalityBattery` — bu dasturda ilmiy shaxsiyat batareyasi (`Standard` + `Scored`
/// metodika) bormi. `false` bo'lsa dastur tugagach shaxsiyat tipi hisoblanmaydi. Mezon —
/// `Domain.Catalog.PersonalityBattery` (metodika KODI bo'yicha qidiruv EMAS). Sessiya
/// darajasidagi ayni shu bayroq `GetSessionStateResult.hasPersonalityBattery` da qaytadi —
/// ikkalasi BIR XIL domen qoidasidan hisoblanadi.
///
/// `Tests` (P52) — aynan shu dasturning test bloklari, `ProgramTest.DisplayOrder` bo'yicha
/// (`ProgramTestCatalog` — sessiya biriktirgan testlar bilan bir xil manba/mezon).
/// </summary>
/// <summary>
/// `RegistrationMode` (P52, 2026-09-11) — `"Full"`/`"None"`. Frontend shunga qarab
/// registratsiya ekranini ko'rsatadi (`Full`) yoki umuman o'tkazib yuboradi (`None`,
/// `POST /sessions` shaxs maydonlarisiz chaqiriladi).
///
/// `RegistrationFields` (P52 kengaytmasi, 2026-09-11, `docs/18` §9.5) — `RegistrationMode = Full`
/// bo'lganda registratsiya ekranidagi HAR BIR maydonning holati (`"Hidden"`/`"Optional"`/
/// `"Required"`); `RegistrationMode = None` bo'lsa mijoz bu maydonni e'tiborsiz qoldiradi
/// (ekran umuman ko'rsatilmaydi). **P52 2-to'lqin (2026-09-12):** endi qiymati GLOBAL
/// `RegistrationFormSettings`dan (dastur ustunligi qo'llangan holda) hisoblanadi —
/// `AssessmentProgram.RegistrationFields` (§9.5, eskirgan) ENDI O'QILMAYDI, lekin shakl
/// (frontend moslashguncha) o'zgarmaydi.
///
/// `RegistrationForm` (P52 2-to'lqin, `docs/18` §9.6.2) — TO'LIQ GLOBAL forma ta'rifi (superadmin
/// qo'shgan "o'z maydonlari" bilan birga), dastur ustunligi QO'LLANGAN holda. `RegistrationFields`
/// bu obyektning `coreFields`ga mos qisqartirilgan (eski shakldagi) proyeksiyasi — ikkalasi BIR
/// XIL manbadan hisoblanadi, hech qachon bir-biriga zid bo'lmaydi.
/// </summary>
public sealed record PublicProgramSummaryDto(
    string Code,
    string NameUz,
    string? DescriptionUz,
    int TestCount,
    int QuestionCount,
    int EstimatedMinutes,
    bool HasPersonalityBattery,
    IReadOnlyList<PublicTestCatalogItemDto> Tests,
    string RegistrationMode,
    RegistrationFieldsDto RegistrationFields,
    RegistrationFormDefinitionDto RegistrationForm);
