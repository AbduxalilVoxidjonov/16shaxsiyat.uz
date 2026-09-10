using StudentRoadMap.Domain.Catalog.Branching;

namespace StudentRoadMap.Application.Admin.Catalog;

/// <summary>
/// `GET /api/admin/catalog/tests` ro'yxat elementi — `docs/07` §3.4. `frontend/src/features/catalog/model/types.ts`
/// dagi `CatalogTestListItem` bilan bir xil maydon nomlari (P37 vazifasi: "javob shakllari
/// o'shalarga mos bo'lsin"). <c>Kind</c> domendagi <see cref="StudentRoadMap.Domain.Catalog.TestKind"/>
/// enumining o'zi ("Standard"/"Custom") EMAS — frontend "System"/"Custom" kutadi, shu sabab
/// <c>CatalogMapping</c> aniq xaritalaydi.
/// </summary>
public sealed record CatalogTestListItemDto(
    Guid Id,
    string Code,
    string NameUz,
    string Kind,
    bool IsSystem,
    string Status,
    bool IsActive,
    string ScoringMode,
    int QuestionCount,
    int ScaleCount,
    int EstimatedMinutes,
    int Version,
    int UsedInProgramCount);

/// <summary>
/// `GET /api/admin/catalog/tests/{id}` — `CatalogTestListItem` + `descriptionUz`/`pageSize`/
/// `shuffleQuestions`/`displayOrder` (frontend `CatalogTestDetail`).
/// <para>
/// <c>DisplayOrder</c> ATAYLAB detal javobida ham bor: `PUT /tests/{id}` uni MAJBURIY talab
/// qiladi (`UpdateCatalogTestCommand.DisplayOrder`), shu sabab uni qaytarmaslik admin UI'ni
/// "joriy tartibni bilmayman" holatiga tushirar va har saqlashda tartibni tasodifiy qiymatga
/// o'zgartirar edi. Ro'yxat DTO'siga (`CatalogTestListItemDto`) qo'shilmadi — u yerda tartib
/// allaqachon qatorlar ketma-ketligida ko'rinadi.
/// </para>
/// </summary>
public sealed record CatalogTestDetailDto(
    Guid Id,
    string Code,
    string NameUz,
    string Kind,
    bool IsSystem,
    string Status,
    bool IsActive,
    string ScoringMode,
    int QuestionCount,
    int ScaleCount,
    int EstimatedMinutes,
    int Version,
    int UsedInProgramCount,
    string? DescriptionUz,
    int PageSize,
    bool ShuffleQuestions,
    int DisplayOrder);

/// <summary>
/// `GET /api/admin/catalog/tests/{id}/questions` — frontend `CatalogQuestionItem` bilan bir xil
/// maydonlar (+ `textRu`/`textEn`/`isSystem`, frontendda ishlatilmaydi, lekin javobni buzmaydi).
///
/// <para>
/// <b><see cref="ScaleNameUz"/> — FAQAT admin katalogi uchun.</b> `CLAUDE.md` 9-qoidasi
/// `scale`/`scaleDirection`ni o'quvchi API'siga chiqarishni taqiqlaydi; nom undan ham
/// xavfliroq — u o'lchanayotgan konstruktni OCHIQ aytadi ("Artistik"), ya'ni o'quvchi javobini
/// moslashtirib natijani buzishi mumkin. Shu sabab bu maydon `Application/Public/**` DTO'larida
/// YO'Q va bo'lmaydi ham; yo'qligi `PublicTestQuestionsEndpointTests.GetTestQuestions_JavobVaSwaggerda_ScaleMaydoniYoq`
/// da xom JSON ustidan qulflangan.
/// </para>
/// <para>
/// <c>null</c> — noma'lum shkala kodi (`CatalogScaleNameResolver` ga qarang). Bu xato emas,
/// degradatsiya: frontend shunda faqat kodni ko'rsatadi.
/// </para>
/// <para>
/// <see cref="ScaleDescriptionUz"/> — o'sha manbadan keladigan QISQA izoh (`docs/03` da bo'lsa
/// yoki `TestScale.DescriptionUz`). Tizim metodikasi sahifasidagi "Shkalalar" ma'lumot bloki
/// uchun; ko'pincha <c>null</c>.
/// </para>
/// <para>
/// `docs/18` §2.3/§5 kengaytmasi: <see cref="SectionId"/>/<see cref="Visibility"/>/
/// <see cref="Placeholder"/>/<see cref="InputPattern"/>/<see cref="MaxLength"/>/
/// <see cref="MinSelections"/>/<see cref="MaxSelections"/>/<see cref="Options"/> — faqat
/// tegishli savol turlarida to'ldirilgan bo'ladi, aks holda `null`. <c>Visibility</c> shu yerda
/// (ADMIN javobida) OCHIQ — `CLAUDE.md` 9-qoidasi faqat `scale`/`scaleDirection`ni man qiladi,
/// shartning o'zi emas; bu superadmin uchun "qaysi savolga bog'liq" ma'lumoti.
/// </para>
/// </summary>
public sealed record CatalogQuestionItemDto(
    Guid Id,
    string Code,
    int Order,
    string TextUz,
    string? TextRu,
    string? TextEn,
    string Type,
    string Scale,
    int Direction,
    decimal Weight,
    bool IsRequired,
    bool IsActive,
    bool IsSystem,
    string? ScaleNameUz,
    string? ScaleDescriptionUz,
    Guid? SectionId = null,
    string? Placeholder = null,
    string? InputPattern = null,
    int? MaxLength = null,
    int? MinSelections = null,
    int? MaxSelections = null,
    VisibilityRule? Visibility = null,
    IReadOnlyList<CatalogOptionDto>? Options = null);

/// <summary>`SingleChoice`/`ForcedChoice`/`MultiChoice` savol variantlari — admin katalogi (`docs/18` §5).</summary>
public sealed record CatalogOptionDto(Guid Id, string TextUz, int Value, int DisplayOrder);

/// <summary>
/// `POST/PUT .../questions` so'rovidagi variant kirishi — `docs/18` §5: `{ textUz, value,
/// displayOrder }`. Tahrirlashda BERILGAN ro'yxat mavjudlarni TO'LIQ almashtiradi (replace).
/// </summary>
public sealed record QuestionOptionInputDto(string TextUz, int Value, int DisplayOrder);

/// <summary>
/// Anketa bo'limi — `GET/POST/PUT/DELETE .../sections` (`docs/18` §2.2/§5). Faqat `Custom`
/// (`IsSystem = false`) anketalarda mavjud bo'lishi mumkin (B-3) — tizim metodikasida bo'lim
/// hech qachon yaratilmaydi, shu sabab bu DTO tizim anketasi uchun har doim bo'sh ro'yxat.
/// </summary>
public sealed record CatalogSectionItemDto(
    Guid Id,
    Guid TestDefinitionId,
    string Code,
    string TitleUz,
    string? DescriptionUz,
    int DisplayOrder,
    VisibilityRule? Visibility);

/// <summary>`docs/03` §6.1 saqlash shakli: `{ "from":0, "to":33, "label":"Past" }`.</summary>
public sealed record InterpretationBandDto(double From, double To, string Label);

/// <summary>`GET/POST/PUT .../scales` — faqat `Custom` testlarda (`docs/07` §3.4).</summary>
public sealed record CatalogScaleItemDto(
    Guid Id,
    Guid TestDefinitionId,
    string Code,
    string NameUz,
    string? DescriptionUz,
    int DisplayOrder,
    IReadOnlyList<InterpretationBandDto> InterpretationBands,
    int QuestionCount);

/// <summary>
/// `POST .../publish` 400 javobidagi bitta xato — `docs/07` §3.4 namunasi: `{code, scale?,
/// questionCode?, message}`. <see cref="SectionCode"/> — `docs/18` §5 kengaytmasi (masalan
/// `SECTION_EMPTY`) — bo'limga oid xato uchun, savolga oid xatolarda `null`.
/// </summary>
public sealed record PublishIssueDto(string Code, string? Scale, string? QuestionCode, string Message, string? SectionCode = null);

/// <summary>`GET .../preview` — o'quvchi ko'radigan ko'rinish (`docs/07` §3.4: "savollar + shkala yorliqlari"). `scale`/`scaleDirection` savol darajasida YO'Q — bu admin uchun ham "o'quvchi qanday ko'radi" ko'rinishi, xom shkala kodlari emas.</summary>
public sealed record CatalogTestPreviewDto(
    Guid Id,
    string Code,
    string NameUz,
    string? DescriptionUz,
    int EstimatedMinutes,
    int PageSize,
    bool ShuffleQuestions,
    IReadOnlyList<CatalogPreviewQuestionDto> Questions,
    IReadOnlyList<CatalogPreviewScaleDto> Scales);

public sealed record CatalogPreviewQuestionDto(Guid Id, string TextUz, string Type, IReadOnlyList<CatalogPreviewOptionDto> Options);

public sealed record CatalogPreviewOptionDto(Guid Id, string TextUz, int Value);

public sealed record CatalogPreviewScaleDto(string Code, string NameUz, string? DescriptionUz);
