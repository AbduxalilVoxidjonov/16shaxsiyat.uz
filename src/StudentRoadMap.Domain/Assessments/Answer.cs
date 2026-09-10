using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Assessments;

/// <summary>
/// Bitta savolga javob. `(AssessmentTestId, QuestionId)` unikal — upsert
/// <see cref="AssessmentTest.UpsertAnswer"/> orqali (`docs/04` 2.5-bo'lim, `docs/18` §2.7).
/// Savol turiga qarab uchta shakldan (`RawValue`/`TextValue`/`SelectedValues`) AYNAN bittasi
/// to'ldiriladi — aks holda `ANSWER_SHAPE_INVALID`.
/// </summary>
public sealed class Answer : Entity
{
    public Guid AssessmentTestId { get; private set; }

    public Guid QuestionId { get; private set; }

    /// <summary>`Likert5`/`Likert7`/`Binary`/`SingleChoice`/`ForcedChoice` uchun (`docs/18` §2.1).</summary>
    public int? RawValue { get; private set; }

    /// <summary>`ShortText`/`LongText`/`Phone` uchun (`docs/18` §2.1, ≤4000 belgi).</summary>
    public string? TextValue { get; private set; }

    /// <summary>`MultiChoice` uchun — bo'sh ro'yxat "yo'q" degani (`docs/18` §2.7).</summary>
    public IReadOnlyList<int> SelectedValues { get; private set; } = [];

    public Guid? SelectedOptionId { get; private set; }

    public int DurationMs { get; private set; }

    public DateTimeOffset AnsweredAt { get; private set; }

    public int RevisionCount { get; private set; }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private Answer()
    {
    }

    private Answer(
        Guid id,
        Guid assessmentTestId,
        Guid questionId,
        int? rawValue,
        string? textValue,
        IReadOnlyList<int> selectedValues,
        Guid? selectedOptionId,
        int durationMs,
        DateTimeOffset answeredAt)
        : base(id)
    {
        AssessmentTestId = assessmentTestId;
        QuestionId = questionId;
        RawValue = rawValue;
        TextValue = textValue;
        SelectedValues = selectedValues;
        SelectedOptionId = selectedOptionId;
        DurationMs = durationMs;
        AnsweredAt = answeredAt;
        RevisionCount = 0;
    }

    public static Answer Create(
        Guid id,
        Guid assessmentTestId,
        Guid questionId,
        int? rawValue,
        Guid? selectedOptionId,
        int durationMs,
        DateTimeOffset answeredAt,
        string? textValue = null,
        IReadOnlyList<int>? selectedValues = null)
    {
        if (durationMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMs), "Javob berish davomiyligi manfiy bo'lishi mumkin emas.");
        }

        var effectiveSelectedValues = selectedValues ?? [];
        ValidateShape(rawValue, textValue, effectiveSelectedValues);

        return new Answer(id, assessmentTestId, questionId, rawValue, textValue, effectiveSelectedValues, selectedOptionId, durationMs, answeredAt);
    }

    /// <summary>O'quvchi javobini o'zgartirganda chaqiriladi — `RevisionCount` oshiriladi.</summary>
    public void UpdateValue(
        int? rawValue,
        Guid? selectedOptionId,
        int durationMs,
        DateTimeOffset answeredAt,
        string? textValue = null,
        IReadOnlyList<int>? selectedValues = null)
    {
        if (durationMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMs), "Javob berish davomiyligi manfiy bo'lishi mumkin emas.");
        }

        var effectiveSelectedValues = selectedValues ?? [];
        ValidateShape(rawValue, textValue, effectiveSelectedValues);

        RawValue = rawValue;
        TextValue = textValue;
        SelectedValues = effectiveSelectedValues;
        SelectedOptionId = selectedOptionId;
        DurationMs = durationMs;
        AnsweredAt = answeredAt;
        RevisionCount++;
    }

    /// <summary>`docs/18` §2.7 invarianti: uchtadan aynan bittasi to'ldirilgan bo'lishi kerak.</summary>
    private static void ValidateShape(int? rawValue, string? textValue, IReadOnlyList<int> selectedValues)
    {
        var filledCount = (rawValue is not null ? 1 : 0)
            + (!string.IsNullOrWhiteSpace(textValue) ? 1 : 0)
            + (selectedValues.Count > 0 ? 1 : 0);

        if (filledCount != 1)
        {
            throw new DomainException(
                "ANSWER_SHAPE_INVALID",
                "Javobning aynan bitta shakli (qiymat/matn/tanlovlar ro'yxati) to'ldirilishi kerak.");
        }
    }
}
