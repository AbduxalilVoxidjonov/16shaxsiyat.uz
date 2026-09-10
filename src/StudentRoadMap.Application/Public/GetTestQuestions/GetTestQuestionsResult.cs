using StudentRoadMap.Domain.Catalog.Branching;

namespace StudentRoadMap.Application.Public.GetTestQuestions;

/// <summary>
/// `docs/07-api-shartnoma.md` 1.5-bo'lim `200` javob shakli. **Diqqat:** `PublicQuestionDto`da
/// `scale`/`scaleDirection` ATAYLAB YO'Q — `CLAUDE.md` 9-qoida, `prompts/11` eng muhim talabi.
///
/// `Sections` — `docs/18` §4.1: bo'limsiz anketada `null`; bo'lim bo'lsa `page = 1`,
/// `TotalPages = 1` va BARCHA faol savollar bilan qaytadi (sahifalash o'chadi) — tarmoqlanishni
/// mijoz (`shared/lib/visibility.ts`) o'zi hisoblaydi, server filtrlab bermaydi.
/// </summary>
public sealed record GetTestQuestionsResult(
    string TestCode,
    int Page,
    int PageSize,
    int TotalPages,
    int TotalQuestions,
    IReadOnlyList<PublicScaleLabelDto>? ScaleLabels,
    IReadOnlyList<PublicQuestionDto> Questions,
    IReadOnlyList<PublicSectionDto>? Sections = null);

/// <summary>Likert/Binary turidagi savollar uchun shkala yorlig'i (masalan, `1 → "Umuman qo'shilmayman"`).</summary>
public sealed record PublicScaleLabelDto(int Value, string Label);

/// <summary>
/// O'quvchiga ko'rinadigan savol shakli. `Type` — `QuestionType` enum'ining string ko'rinishi
/// (`"Likert5"`, `"SingleChoice"`, `"ShortText"` va h.k.). `CurrentValue`/`CurrentText`/
/// `CurrentValues` — o'quvchi shu savolga avval javob bergan bo'lsa, uning oxirgi qiymati
/// (keshdan EMAS, har chaqiriqda DB'dan yangi o'qiladi) — savol turiga qarab AYNAN bittasi
/// to'ldirilgan bo'ladi (`docs/18` §2.1/§2.7).
///
/// `SectionId`/`Visibility` — `docs/18` §2.2/§2.4 (bo'limsiz/shartsiz savolda `null`).
/// `Placeholder`/`InputPattern`/`MaxLength` — matn turlari (`ShortText`/`LongText`/`Phone`);
/// `MinSelections`/`MaxSelections` — `MultiChoice`. `scale`/`scaleDirection` bu yerda HECH
/// QACHON yo'q (`CLAUDE.md` 9-qoida).
/// </summary>
public sealed record PublicQuestionDto(
    Guid Id,
    string Code,
    int Order,
    string Text,
    string Type,
    bool IsRequired,
    IReadOnlyList<PublicAnswerOptionDto>? Options,
    int? CurrentValue,
    Guid? SectionId = null,
    string? Placeholder = null,
    string? InputPattern = null,
    int? MaxLength = null,
    int? MinSelections = null,
    int? MaxSelections = null,
    VisibilityRule? Visibility = null,
    string? CurrentText = null,
    IReadOnlyList<int>? CurrentValues = null);

/// <summary>`SingleChoice`/`ForcedChoice`/`MultiChoice` savollari uchun tanlov varianti.</summary>
public sealed record PublicAnswerOptionDto(Guid Id, string Text, int Value, int Order);

/// <summary>
/// Anketa bo'limi (`docs/18` §4.1) — `VisibilityRule`ning jsonb shakli bilan AYNAN bir xil
/// wire ko'rinishida chiqadi (`{ match, conditions: [{ questionCode, operator, values }] }`),
/// chunki frontend `shared/lib/visibility.ts` shu shaklni kutadi.
/// </summary>
public sealed record PublicSectionDto(
    Guid Id,
    string Code,
    string Title,
    string? Description,
    int Order,
    VisibilityRule? Visibility);
