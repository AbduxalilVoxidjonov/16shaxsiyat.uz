using FluentAssertions;
using StudentRoadMap.Domain.Assessments;

namespace StudentRoadMap.Domain.Tests.Assessments;

public sealed class AnswerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithNegativeDuration_ThrowsArgumentOutOfRangeException()
    {
        var act = () => Answer.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, null, -1, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_SetsRevisionCountToZero()
    {
        var answer = Answer.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);

        answer.RevisionCount.Should().Be(0);
    }

    [Fact]
    public void UpdateValue_IncrementsRevisionCount()
    {
        var answer = Answer.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);

        answer.UpdateValue(5, null, 1500, Now.AddSeconds(30));
        answer.UpdateValue(2, null, 1800, Now.AddSeconds(60));

        answer.RevisionCount.Should().Be(2);
        answer.RawValue.Should().Be(2);
        answer.DurationMs.Should().Be(1800);
    }

    [Fact]
    public void UpdateValue_WithNegativeDuration_ThrowsArgumentOutOfRangeException()
    {
        var answer = Answer.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);

        var act = () => answer.UpdateValue(3, null, -5, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
