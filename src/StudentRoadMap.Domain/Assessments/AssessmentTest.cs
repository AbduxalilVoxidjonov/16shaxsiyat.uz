using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Assessments;

/// <summary>
/// Sessiya ichidagi bitta test bloki (masalan, MBTI-16). `Assessment` agregati tarkibida
/// yashaydi (`docs/04` 2.4-bo'lim).
/// </summary>
public sealed class AssessmentTest : Entity
{
    private readonly List<Answer> _answers = [];

    private List<Guid> _questionOrder = [];

    public Guid AssessmentId { get; private set; }

    public Guid TestDefinitionId { get; private set; }

    public TestStatus Status { get; private set; }

    public int DisplayOrder { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public int AnsweredCount { get; private set; }

    public int TotalCount { get; private set; }

    /// <summary>Aralashtirilgan savol tartibi — qayta kirganda bir xil bo'lishi uchun.</summary>
    public IReadOnlyList<Guid> QuestionOrder => _questionOrder.AsReadOnly();

    public IReadOnlyCollection<Answer> Answers => _answers.AsReadOnly();

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private AssessmentTest()
    {
    }

    private AssessmentTest(Guid id, Guid assessmentId, Guid testDefinitionId, int displayOrder, int totalCount)
        : base(id)
    {
        AssessmentId = assessmentId;
        TestDefinitionId = testDefinitionId;
        DisplayOrder = displayOrder;
        TotalCount = totalCount;
        AnsweredCount = 0;
        Status = TestStatus.NotStarted;
    }

    public static AssessmentTest Create(Guid id, Guid assessmentId, Guid testDefinitionId, int displayOrder, int totalCount)
    {
        if (totalCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount), "Test savollari soni musbat bo'lishi kerak.");
        }

        return new AssessmentTest(id, assessmentId, testDefinitionId, displayOrder, totalCount);
    }

    /// <summary>Qayta kirganda bir xil bo'lishi uchun aralashtirilgan savol tartibini belgilaydi.</summary>
    public void SetQuestionOrder(IReadOnlyList<Guid> questionOrder)
    {
        if (Status == TestStatus.Completed)
        {
            throw new DomainException("ASSESSMENT_TEST_ALREADY_COMPLETED", "Yakunlangan test uchun savol tartibini o'zgartirib bo'lmaydi.");
        }

        _questionOrder = [.. questionOrder];
    }

    public void Start(DateTimeOffset now)
    {
        switch (Status)
        {
            case TestStatus.NotStarted:
                Status = TestStatus.InProgress;
                StartedAt = now;
                return;
            case TestStatus.InProgress:
                return; // idempotent
            default:
                throw new DomainException("ASSESSMENT_TEST_INVALID_TRANSITION", $"Test '{Status}' holatida qayta boshlanmaydi.");
        }
    }

    public void Complete(DateTimeOffset now)
    {
        if (Status != TestStatus.InProgress)
        {
            throw new DomainException("ASSESSMENT_TEST_INVALID_TRANSITION", $"Test '{Status}' holatida yakunlanmaydi.");
        }

        if (AnsweredCount < TotalCount)
        {
            throw new DomainException("ASSESSMENT_TEST_NOT_ANSWERED", "Barcha savollarga javob berilmasdan turib testni yakunlab bo'lmaydi.");
        }

        Status = TestStatus.Completed;
        CompletedAt = now;
    }

    /// <summary>
    /// Javobni qo'shadi yoki mavjudini yangilaydi — `(AssessmentTestId, QuestionId)` unikal (upsert).
    /// </summary>
    public void UpsertAnswer(
        Guid answerId,
        Guid questionId,
        int rawValue,
        Guid? selectedOptionId,
        int durationMs,
        DateTimeOffset answeredAt)
    {
        if (Status != TestStatus.InProgress)
        {
            throw new DomainException("ASSESSMENT_TEST_NOT_IN_PROGRESS", "Faqat boshlangan testga javob yozish mumkin.");
        }

        var existing = _answers.FirstOrDefault(a => a.QuestionId == questionId);
        if (existing is not null)
        {
            existing.UpdateValue(rawValue, selectedOptionId, durationMs, answeredAt);
            return;
        }

        _answers.Add(Answer.Create(answerId, Id, questionId, rawValue, selectedOptionId, durationMs, answeredAt));
        AnsweredCount++;
    }
}
