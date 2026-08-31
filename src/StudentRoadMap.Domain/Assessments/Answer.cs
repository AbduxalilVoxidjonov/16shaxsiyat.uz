using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Assessments;

/// <summary>
/// Bitta savolga javob. `(AssessmentTestId, QuestionId)` unikal — upsert
/// <see cref="AssessmentTest.UpsertAnswer"/> orqali (`docs/04` 2.5-bo'lim).
/// </summary>
public sealed class Answer : Entity
{
    public Guid AssessmentTestId { get; private set; }

    public Guid QuestionId { get; private set; }

    public int RawValue { get; private set; }

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
        int rawValue,
        Guid? selectedOptionId,
        int durationMs,
        DateTimeOffset answeredAt)
        : base(id)
    {
        AssessmentTestId = assessmentTestId;
        QuestionId = questionId;
        RawValue = rawValue;
        SelectedOptionId = selectedOptionId;
        DurationMs = durationMs;
        AnsweredAt = answeredAt;
        RevisionCount = 0;
    }

    public static Answer Create(
        Guid id,
        Guid assessmentTestId,
        Guid questionId,
        int rawValue,
        Guid? selectedOptionId,
        int durationMs,
        DateTimeOffset answeredAt)
    {
        if (durationMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMs), "Javob berish davomiyligi manfiy bo'lishi mumkin emas.");
        }

        return new Answer(id, assessmentTestId, questionId, rawValue, selectedOptionId, durationMs, answeredAt);
    }

    /// <summary>O'quvchi javobini o'zgartirganda chaqiriladi — `RevisionCount` oshiriladi.</summary>
    public void UpdateValue(int rawValue, Guid? selectedOptionId, int durationMs, DateTimeOffset answeredAt)
    {
        if (durationMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMs), "Javob berish davomiyligi manfiy bo'lishi mumkin emas.");
        }

        RawValue = rawValue;
        SelectedOptionId = selectedOptionId;
        DurationMs = durationMs;
        AnsweredAt = answeredAt;
        RevisionCount++;
    }
}
