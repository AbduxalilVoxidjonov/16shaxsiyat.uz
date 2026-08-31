using FluentAssertions;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Events;

namespace StudentRoadMap.Domain.Tests.Assessments;

/// <summary>
/// `Assessment` holat mashinasi (`docs/04` 2.3-bo'lim) — to'g'ri va noto'g'ri o'tishlar.
/// </summary>
public sealed class AssessmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static Assessment CreateDraftAssessment(DateTimeOffset now) => Assessment.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "session-token-abc",
        "uz",
        now,
        now.AddDays(7),
        now);

    private static (Assessment assessment, Guid testDefId1, Guid testDefId2) CreateAssessmentWithTwoTests(DateTimeOffset now)
    {
        var assessment = CreateDraftAssessment(now);
        var testDefId1 = Guid.NewGuid();
        var testDefId2 = Guid.NewGuid();

        assessment.AddTest(AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testDefId1, 1, 2));
        assessment.AddTest(AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testDefId2, 2, 1));

        return (assessment, testDefId1, testDefId2);
    }

    private static void CompleteTest(Assessment assessment, Guid testDefinitionId, int answersNeeded, DateTimeOffset now)
    {
        assessment.StartTest(testDefinitionId, now);
        var test = assessment.Tests.Single(t => t.TestDefinitionId == testDefinitionId);

        for (var i = 0; i < answersNeeded; i++)
        {
            test.UpsertAnswer(Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, now);
        }

        assessment.CompleteTest(testDefinitionId, now);
    }

    private static Assessment CreateCompletedAssessment(DateTimeOffset now)
    {
        var (assessment, testDefId1, testDefId2) = CreateAssessmentWithTwoTests(now);
        CompleteTest(assessment, testDefId1, 2, now);
        CompleteTest(assessment, testDefId2, 1, now);
        assessment.Complete(now);
        return assessment;
    }

    // ---------- Create ----------

    [Fact]
    public void Create_SetsDraftStatusAndRaisesAssessmentStartedEvent()
    {
        var assessment = CreateDraftAssessment(Now);

        assessment.Status.Should().Be(AssessmentStatus.Draft);
        assessment.DomainEvents.Should().ContainSingle(e => e is AssessmentStartedEvent);
    }

    [Fact]
    public void Create_WithEmptySessionToken_ThrowsArgumentException()
    {
        var act = () => Assessment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), " ", "uz", Now, Now.AddDays(7), Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithExpiresAtNotAfterStartedAt_ThrowsArgumentException()
    {
        var act = () => Assessment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "token", "uz", Now, Now, Now);

        act.Should().Throw<ArgumentException>();
    }

    // ---------- AddTest ----------

    [Fact]
    public void AddTest_WithDuplicateTestDefinition_ThrowsDomainException()
    {
        var assessment = CreateDraftAssessment(Now);
        var testDefId = Guid.NewGuid();
        assessment.AddTest(AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testDefId, 1, 2));

        var act = () => assessment.AddTest(AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testDefId, 2, 2));

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("ASSESSMENT_TEST_DUPLICATE");
    }

    // ---------- Valid transitions ----------

    [Fact]
    public void StartTest_FromDraft_TransitionsToInProgress()
    {
        var (assessment, testDefId1, _) = CreateAssessmentWithTwoTests(Now);

        assessment.StartTest(testDefId1, Now);

        assessment.Status.Should().Be(AssessmentStatus.InProgress);
        assessment.Tests.Single(t => t.TestDefinitionId == testDefId1).Status.Should().Be(TestStatus.InProgress);
    }

    [Fact]
    public void StartTest_WhenAlreadyInProgress_IsIdempotentForNewTest()
    {
        var (assessment, testDefId1, testDefId2) = CreateAssessmentWithTwoTests(Now);
        assessment.StartTest(testDefId1, Now);

        var act = () => assessment.StartTest(testDefId2, Now);

        act.Should().NotThrow();
        assessment.Status.Should().Be(AssessmentStatus.InProgress);
    }

    [Fact]
    public void CompleteTest_RaisesTestCompletedEvent()
    {
        var (assessment, testDefId1, _) = CreateAssessmentWithTwoTests(Now);
        assessment.StartTest(testDefId1, Now);
        var test = assessment.Tests.Single(t => t.TestDefinitionId == testDefId1);
        test.UpsertAnswer(Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);
        test.UpsertAnswer(Guid.NewGuid(), Guid.NewGuid(), 4, null, 1000, Now);

        assessment.CompleteTest(testDefId1, Now);

        assessment.DomainEvents.Should().ContainSingle(e => e is TestCompletedEvent);
    }

    [Fact]
    public void Complete_WhenAllTestsCompleted_TransitionsToCompletedAndRaisesEvent()
    {
        var (assessment, testDefId1, testDefId2) = CreateAssessmentWithTwoTests(Now);
        CompleteTest(assessment, testDefId1, 2, Now);
        CompleteTest(assessment, testDefId2, 1, Now);
        var completionTime = Now.AddMinutes(20);

        assessment.Complete(completionTime);

        assessment.Status.Should().Be(AssessmentStatus.Completed);
        assessment.CompletedAt.Should().Be(completionTime);
        assessment.TotalDurationSeconds.Should().Be((int)(completionTime - Now).TotalSeconds);
        assessment.DomainEvents.Should().Contain(e => e is AssessmentCompletedEvent);
    }

    [Theory]
    [MemberData(nameof(StatesAllowingMarkAnalyzing))]
    public void MarkAnalyzing_FromAllowedState_TransitionsToAnalyzing(AssessmentStatus fromStatus)
    {
        var assessment = BuildAssessmentInStatus(fromStatus);

        assessment.MarkAnalyzing(Now.AddHours(1));

        assessment.Status.Should().Be(AssessmentStatus.Analyzing);
    }

    public static TheoryData<AssessmentStatus> StatesAllowingMarkAnalyzing() =>
    [
        AssessmentStatus.Completed,
        AssessmentStatus.Analyzed,
        AssessmentStatus.AnalysisFailed,
    ];

    [Fact]
    public void MarkAnalyzed_FromAnalyzing_TransitionsToAnalyzed()
    {
        var assessment = BuildAssessmentInStatus(AssessmentStatus.Analyzing);

        assessment.MarkAnalyzed(Now.AddHours(2));

        assessment.Status.Should().Be(AssessmentStatus.Analyzed);
    }

    [Fact]
    public void MarkAnalysisFailed_FromAnalyzing_TransitionsToAnalysisFailed()
    {
        var assessment = BuildAssessmentInStatus(AssessmentStatus.Analyzing);

        assessment.MarkAnalysisFailed(Now.AddHours(2));

        assessment.Status.Should().Be(AssessmentStatus.AnalysisFailed);
    }

    [Fact]
    public void MarkAbandoned_FromDraft_TransitionsToAbandoned()
    {
        var assessment = CreateDraftAssessment(Now);

        assessment.MarkAbandoned(Now.AddDays(8));

        assessment.Status.Should().Be(AssessmentStatus.Abandoned);
    }

    [Fact]
    public void MarkAbandoned_FromInProgress_TransitionsToAbandoned()
    {
        var (assessment, testDefId1, _) = CreateAssessmentWithTwoTests(Now);
        assessment.StartTest(testDefId1, Now);

        assessment.MarkAbandoned(Now.AddDays(8));

        assessment.Status.Should().Be(AssessmentStatus.Abandoned);
    }

    // ---------- Invalid transitions (kamida 6 ta, docs/04 2.3 holat mashinasi) ----------

    [Fact]
    public void Complete_FromDraft_ThrowsDomainExceptionWithNonEmptyCode()
    {
        var assessment = CreateDraftAssessment(Now);

        var act = () => assessment.Complete(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().NotBeNullOrEmpty();
        ex.Code.Should().Be("ASSESSMENT_INVALID_TRANSITION");
    }

    [Fact]
    public void Complete_WhenTestsNotDone_ThrowsDomainExceptionWithTestsNotDoneCode()
    {
        var (assessment, testDefId1, _) = CreateAssessmentWithTwoTests(Now);
        assessment.StartTest(testDefId1, Now); // InProgress, lekin hech qaysi test yakunlanmagan

        var act = () => assessment.Complete(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("ASSESSMENT_TESTS_NOT_DONE");
    }

    [Fact]
    public void StartTest_FromCompleted_ThrowsDomainException()
    {
        var assessment = CreateCompletedAssessment(Now);

        var act = () => assessment.StartTest(Guid.NewGuid(), Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void StartTest_FromAbandoned_ThrowsDomainException()
    {
        var assessment = CreateDraftAssessment(Now);
        assessment.MarkAbandoned(Now.AddDays(8));

        var act = () => assessment.StartTest(Guid.NewGuid(), Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MarkAnalyzing_FromDraft_ThrowsDomainException()
    {
        var assessment = CreateDraftAssessment(Now);

        var act = () => assessment.MarkAnalyzing(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MarkAnalyzed_FromDraft_ThrowsDomainException()
    {
        var assessment = CreateDraftAssessment(Now);

        var act = () => assessment.MarkAnalyzed(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MarkAnalysisFailed_FromCompleted_ThrowsDomainException()
    {
        var assessment = CreateCompletedAssessment(Now);

        var act = () => assessment.MarkAnalysisFailed(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MarkAbandoned_FromCompleted_ThrowsDomainException()
    {
        var assessment = CreateCompletedAssessment(Now);

        var act = () => assessment.MarkAbandoned(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MarkAbandoned_FromAbandoned_ThrowsDomainException()
    {
        var assessment = CreateDraftAssessment(Now);
        assessment.MarkAbandoned(Now.AddDays(8));

        var act = () => assessment.MarkAbandoned(Now.AddDays(9));

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Complete_FromAnalyzing_ThrowsDomainException()
    {
        var assessment = BuildAssessmentInStatus(AssessmentStatus.Analyzing);

        var act = () => assessment.Complete(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MarkAnalyzed_FromAlreadyAnalyzed_ThrowsDomainException()
    {
        var assessment = BuildAssessmentInStatus(AssessmentStatus.Analyzed);

        var act = () => assessment.MarkAnalyzed(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().NotBeNullOrEmpty();
    }

    // ---------- Yordamchi metodlar ----------

    /// <summary>Berilgan holatga yetkazilgan sessiya quradi (faqat Completed'dan yuqori bosqichlar uchun).</summary>
    private static Assessment BuildAssessmentInStatus(AssessmentStatus status)
    {
        var assessment = CreateCompletedAssessment(Now);

        if (status == AssessmentStatus.Completed)
        {
            return assessment;
        }

        assessment.MarkAnalyzing(Now.AddHours(1));

        if (status == AssessmentStatus.Analyzing)
        {
            return assessment;
        }

        if (status == AssessmentStatus.Analyzed)
        {
            assessment.MarkAnalyzed(Now.AddHours(2));
            return assessment;
        }

        if (status == AssessmentStatus.AnalysisFailed)
        {
            assessment.MarkAnalysisFailed(Now.AddHours(2));
            return assessment;
        }

        throw new ArgumentOutOfRangeException(nameof(status), status, "Testda qo'llab-quvvatlanmaydigan holat.");
    }
}
