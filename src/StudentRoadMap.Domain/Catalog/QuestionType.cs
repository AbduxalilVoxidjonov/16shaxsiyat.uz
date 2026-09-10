namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// Savol turi — qiymatlar `docs/05-database-schema.md` 3-bo'limiga mos. `ShortText`/`LongText`/
/// `MultiChoice`/`Phone` — P52 tarmoqlanuvchi so'rovnoma (`docs/18` §2.1), FAQAT
/// `TestScoringMode.Survey` anketalarda ruxsat etilgan (B-1, `QUESTION_TYPE_NOT_SCORABLE`).
/// </summary>
public enum QuestionType
{
    Likert5 = 1,
    Likert7 = 2,
    Binary = 3,
    SingleChoice = 4,
    ForcedChoice = 5,

    /// <summary>Bir qatorli matn (`docs/18` §2.1). `RawValue` yo'q — javob `TextValue`da.</summary>
    ShortText = 6,

    /// <summary>Ko'p qatorli matn (`textarea`). `RawValue` yo'q — javob `TextValue`da.</summary>
    LongText = 7,

    /// <summary>Bir nechta variant tanlanadi (checkbox). `RawValue` yo'q — javob `SelectedValues`da.</summary>
    MultiChoice = 8,

    /// <summary>`ShortText`ning maxsus holati — standart `InputPattern`/`Placeholder` O'zbekiston raqami uchun.</summary>
    Phone = 9,
}
