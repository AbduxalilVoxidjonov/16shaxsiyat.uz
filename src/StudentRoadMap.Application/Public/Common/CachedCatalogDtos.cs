using StudentRoadMap.Domain.Catalog;

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
public sealed record CachedTestDefinitionDto(Guid Id, string Code, int PageSize, bool ShuffleQuestions, int Version, string ScoringStrategyCode);

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
public sealed record CachedQuestionDto(
    Guid Id,
    string Code,
    int DisplayOrder,
    string Text,
    QuestionType QuestionType,
    bool IsRequired,
    IReadOnlyList<CachedAnswerOptionDto> Options);

/// <summary>`SingleChoice`/`ForcedChoice` savollari uchun keshlangan variant (hozircha faqat uz — variant matni tilga bog'lanmagan, `AnswerOption`da Ru/En maydoni yo'q).</summary>
public sealed record CachedAnswerOptionDto(Guid Id, string TextUz, int Value, int DisplayOrder);
