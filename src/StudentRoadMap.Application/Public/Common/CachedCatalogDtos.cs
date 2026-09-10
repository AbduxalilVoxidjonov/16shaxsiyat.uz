using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Catalog.Branching;

namespace StudentRoadMap.Application.Public.Common;

/// <summary>
/// `PublicCatalogCache` orqali 10 daqiqaga keshlanadigan `TestDefinition` proyeksiyasi
/// (`prompts/11`). Faqat ommaviy oqim uchun kerakli maydonlar — `scale`/`scaleDirection`
/// bu yerda YO'Q (`CLAUDE.md` 9-qoida). `Version`/`ScoringStrategyCode` `prompts/12` uchun
/// qo'shildi — `CompleteTestCommandHandler` `ScoringEngine.Score(strategyCode, ...)`ni
/// chaqirish va `TestResult.TestVersion`ni yozish uchun ishlatadi (`docs/03` §8: "Formula
/// o'zgarsa `TestDefinition.Version` oshiriladi... `TestResult.ScoringVersion`da qaysi versiya
/// ishlatilgani qoladi").
/// </summary>
/// <summary>
/// `ScoringStrategyCode` — `Survey` (`ScoringMode`) rejimida `null` (`docs/06` 8-bo'lim,
/// 2026-09-02 qaror, `prompts/34` A4-band): ballanmaydi, strategiya ishlatilmaydi.
/// </summary>
public sealed record CachedTestDefinitionDto(Guid Id, string Code, int PageSize, bool ShuffleQuestions, int Version, string? ScoringStrategyCode, TestScoringMode ScoringMode);

/// <summary>
/// Keshlangan savol proyeksiyasi. **Diqqat:** `Scale`/`ScaleDirection`/`Weight` ATAYLAB bu
/// yerda YO'Q — chunki keshdan to'g'ridan-to'g'ri javob DTO'siga proyeksiya qilinadi, bu tur
/// hech qachon shu maydonlarni tashimasligi kompilyatsiya darajasida kafolatlanadi.
/// `currentValue` (o'quvchiga xos javob) BU YERDA HECH QACHON bo'lmaydi — u har chaqiriqda
/// alohida, kesh tashqarisida qo'shiladi.
///
/// `Text` — `PublicCatalogCache.GetActiveQuestionsAsync` chaqirilgan `languageCode` uchun
/// ALLAQACHON tanlangan matn (`Question.TextRu`/`TextEn` mos kelmasa `TextUz`ga qaytadi —
/// QA topilmasi, 2026-09-02: bu tanlash kesh ICHIDA, tilga xos kesh yozuvida bajariladi,
/// chunki kesh KALITI `languageCode`ni o'z ichiga oladi — bir tilda ishlagan javob boshqa
/// tildagi o'quvchiga sizib chiqmasligi uchun).
/// </summary>
/// <summary>
/// P52 (`docs/18` §2.2/§2.3): `SectionId`/`Visibility`/`Placeholder`/`InputPattern`/`MaxLength`/
/// `MinSelections`/`MaxSelections` — tarmoqlanuvchi so'rovnoma maydonlari. `Visibility` — `Scale`
/// EMAS (`CLAUDE.md` 9-qoida faqat shkalaga tegishli), shu sabab bu yerda bo'lishi xavfsiz.
/// </summary>
public sealed record CachedQuestionDto(
    Guid Id,
    string Code,
    int DisplayOrder,
    string Text,
    QuestionType QuestionType,
    bool IsRequired,
    IReadOnlyList<CachedAnswerOptionDto> Options,
    Guid? SectionId,
    VisibilityRule? Visibility,
    string? Placeholder,
    string? InputPattern,
    int? MaxLength,
    int? MinSelections,
    int? MaxSelections);

/// <summary>`SingleChoice`/`ForcedChoice`/`MultiChoice` savollari uchun keshlangan variant (hozircha faqat uz — variant matni tilga bog'lanmagan, `AnswerOption`da Ru/En maydoni yo'q).</summary>
public sealed record CachedAnswerOptionDto(Guid Id, string TextUz, int Value, int DisplayOrder);

/// <summary>
/// Keshlangan bo'lim (`docs/18` §2.2) — `PublicCatalogCache.GetSectionsAsync`. Tilga bog'liq
/// EMAS (`QuestionSection`da `TitleRu`/`TitleEn` yo'q, faqat `TitleUz`/`DescriptionUz`).
/// </summary>
public sealed record CachedSectionDto(
    Guid Id,
    string Code,
    string TitleUz,
    string? DescriptionUz,
    int DisplayOrder,
    VisibilityRule? Visibility);

/// <summary>
/// `type_catalog` yozuvining ommaviy (marketing) proyeksiyasi — `PublicCatalogCache.GetTypeCatalogAsync`
/// orqali keshlanadi. Kontent butunlay `SeedData/type-catalog.json` dan keladi (loyihaning
/// o'z o'zbekcha matni), shu sabab bu yerda hech qanday matn hardcode qilinmaydi.
///
/// O'quvchiga xos yoki maxfiy hech narsa yo'q — bu yozuv ochiq sahifada (`/metodika`) ko'rsatiladi,
/// shu sabab uni sessiyaga bog'liq bo'lmagan umumiy keshda saqlash xavfsiz.
/// </summary>
public sealed record CachedTypeCatalogEntryDto(
    string Code,
    string Name,
    string ShortDescription,
    string LongDescription,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> GrowthAreas,
    IReadOnlyList<string> CareerHints);
