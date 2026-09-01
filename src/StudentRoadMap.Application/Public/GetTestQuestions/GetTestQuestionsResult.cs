namespace StudentRoadMap.Application.Public.GetTestQuestions;

/// <summary>
/// `docs/07-api-shartnoma.md` 1.5-bo'lim `200` javob shakli. **Diqqat:** `PublicQuestionDto`da
/// `scale`/`scaleDirection` ATAYLAB YO'Q — `CLAUDE.md` 9-qoida, `prompts/11` eng muhim talabi.
/// </summary>
public sealed record GetTestQuestionsResult(
    string TestCode,
    int Page,
    int PageSize,
    int TotalPages,
    int TotalQuestions,
    IReadOnlyList<PublicScaleLabelDto>? ScaleLabels,
    IReadOnlyList<PublicQuestionDto> Questions);

/// <summary>Likert/Binary turidagi savollar uchun shkala yorlig'i (masalan, `1 → "Umuman qo'shilmayman"`).</summary>
public sealed record PublicScaleLabelDto(int Value, string Label);

/// <summary>
/// O'quvchiga ko'rinadigan savol shakli. `Type` — `QuestionType` enum'ining string ko'rinishi
/// (`"Likert5"`, `"SingleChoice"` va h.k.). `CurrentValue` — o'quvchi shu savolga avval javob
/// bergan bo'lsa, uning oxirgi qiymati (keshdan EMAS, har chaqiriqda DB'dan yangi o'qiladi).
/// </summary>
public sealed record PublicQuestionDto(
    Guid Id,
    string Code,
    int Order,
    string Text,
    string Type,
    bool IsRequired,
    IReadOnlyList<PublicAnswerOptionDto>? Options,
    int? CurrentValue);

/// <summary>`SingleChoice`/`ForcedChoice` savollari uchun tanlov varianti.</summary>
public sealed record PublicAnswerOptionDto(Guid Id, string Text, int Value, int Order);
