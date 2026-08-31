using FluentAssertions;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Tests.Assessments;

public sealed class AssessmentTestTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static AssessmentTest CreateTest(int totalCount = 2) =>
        AssessmentTest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, totalCount);

    [Fact]
    public void Create_WithNonPositiveTotalCount_ThrowsArgumentOutOfRangeException()
    {
        var act = () => AssessmentTest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Start_FromNotStarted_TransitionsToInProgress()
    {
        var test = CreateTest();

        test.Start(Now);

        test.Status.Should().Be(TestStatus.InProgress);
        test.StartedAt.Should().Be(Now);
    }

    [Fact]
    public void Start_WhenAlreadyInProgress_IsIdempotent()
    {
        var test = CreateTest();
        test.Start(Now);

        var act = () => test.Start(Now.AddMinutes(1));

        act.Should().NotThrow();
        test.StartedAt.Should().Be(Now); // qayta chaqirilganda o'zgarmaydi
    }

    [Fact]
    public void Start_WhenAlreadyCompleted_ThrowsDomainException()
    {
        var test = CreateTest(1);
        test.Start(Now);
        test.UpsertAnswer(Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);
        test.Complete(Now);

        var act = () => test.Start(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Complete_WhenNotAllQuestionsAnswered_ThrowsDomainException()
    {
        var test = CreateTest(2);
        test.Start(Now);
        test.UpsertAnswer(Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now); // faqat 1/2

        var act = () => test.Complete(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("ASSESSMENT_TEST_NOT_ANSWERED");
    }

    [Fact]
    public void Complete_WhenNotInProgress_ThrowsDomainException()
    {
        var test = CreateTest();

        var act = () => test.Complete(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("ASSESSMENT_TEST_INVALID_TRANSITION");
    }

    [Fact]
    public void Complete_WhenAllQuestionsAnswered_TransitionsToCompleted()
    {
        var test = CreateTest(2);
        test.Start(Now);
        test.UpsertAnswer(Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);
        test.UpsertAnswer(Guid.NewGuid(), Guid.NewGuid(), 4, null, 1000, Now);

        test.Complete(Now.AddMinutes(5));

        test.Status.Should().Be(TestStatus.Completed);
        test.CompletedAt.Should().Be(Now.AddMinutes(5));
    }

    [Fact]
    public void UpsertAnswer_WhenNotInProgress_ThrowsDomainException()
    {
        var test = CreateTest();

        var act = () => test.UpsertAnswer(Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("ASSESSMENT_TEST_NOT_IN_PROGRESS");
    }

    [Fact]
    public void UpsertAnswer_WithNewQuestion_IncreasesAnsweredCount()
    {
        var test = CreateTest();
        test.Start(Now);

        test.UpsertAnswer(Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);

        test.AnsweredCount.Should().Be(1);
        test.Answers.Should().HaveCount(1);
    }

    [Fact]
    public void UpsertAnswer_WithExistingQuestion_UpdatesInPlaceWithoutIncreasingAnsweredCount()
    {
        var test = CreateTest();
        test.Start(Now);
        var questionId = Guid.NewGuid();
        test.UpsertAnswer(Guid.NewGuid(), questionId, 3, null, 1000, Now);

        test.UpsertAnswer(Guid.NewGuid(), questionId, 5, null, 1500, Now.AddSeconds(10));

        test.AnsweredCount.Should().Be(1);
        test.Answers.Should().ContainSingle();
        test.Answers.Single().RawValue.Should().Be(5);
        test.Answers.Single().RevisionCount.Should().Be(1);
    }

    [Fact]
    public void SetQuestionOrder_WhenCompleted_ThrowsDomainException()
    {
        var test = CreateTest(1);
        test.Start(Now);
        test.UpsertAnswer(Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);
        test.Complete(Now);

        var act = () => test.SetQuestionOrder([Guid.NewGuid()]);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("ASSESSMENT_TEST_ALREADY_COMPLETED");
    }

    [Fact]
    public void SetQuestionOrder_WhenNotCompleted_SetsOrder()
    {
        var test = CreateTest();
        var order = new[] { Guid.NewGuid(), Guid.NewGuid() };

        test.SetQuestionOrder(order);

        test.QuestionOrder.Should().Equal(order);
    }
}
