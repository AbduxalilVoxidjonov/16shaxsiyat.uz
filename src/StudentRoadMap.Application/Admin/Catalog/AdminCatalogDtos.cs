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

/// <summary>`GET /api/admin/catalog/tests/{id}` — `CatalogTestListItem` + `descriptionUz`/`pageSize`/`shuffleQuestions` (frontend `CatalogTestDetail`).</summary>
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
    bool ShuffleQuestions);

/// <summary>`GET /api/admin/catalog/tests/{id}/questions` — frontend `CatalogQuestionItem` bilan bir xil maydonlar (+ `textRu`/`textEn`/`isSystem`, frontendda ishlatilmaydi, lekin javobni buzmaydi).</summary>
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
    bool IsSystem);

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

/// <summary>`POST .../publish` 400 javobidagi bitta xato — `docs/07` §3.4 namunasi: `{code, scale?, questionCode?, message}`.</summary>
public sealed record PublishIssueDto(string Code, string? Scale, string? QuestionCode, string Message);

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
