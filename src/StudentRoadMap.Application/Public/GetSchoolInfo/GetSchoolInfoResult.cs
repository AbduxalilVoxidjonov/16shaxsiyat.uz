namespace StudentRoadMap.Application.Public.GetSchoolInfo;

/// <summary>
/// `docs/07-api-shartnoma.md` 1.1-bo'lim javob shakli. `Programs` — `prompts/34` C8-band
/// bilan qo'shildi: maktab uchun mavjud dasturlar (`Visibility = Public` yoki biriktirilgan).
/// `Tests` ORQAGA MOSLIK uchun saqlanadi (barcha nashr qilingan/faol testlar, dasturdan
/// qat'i nazar) — mavjud mijoz/test kodi buzilmasligi uchun.
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

/// <summary>Boshlanish ekranidagi bitta test bloki haqida ma'lumot (savol soni, taxminiy vaqt).</summary>
public sealed record PublicTestCatalogItemDto(
    string Code,
    string Name,
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
/// </summary>
public sealed record PublicProgramSummaryDto(
    string Code,
    string NameUz,
    string? DescriptionUz,
    int TestCount,
    int QuestionCount,
    int EstimatedMinutes,
    bool HasPersonalityBattery);
