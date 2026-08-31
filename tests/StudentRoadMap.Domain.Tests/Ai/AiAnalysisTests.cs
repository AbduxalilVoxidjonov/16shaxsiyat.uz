using FluentAssertions;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Events;

namespace StudentRoadMap.Domain.Tests.Ai;

/// <summary>`AiAnalysis` holat mashinasi: `Pending → Running → Succeeded/Failed` (`docs/04` 2.8-bo'lim).</summary>
public sealed class AiAnalysisTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static AiAnalysis CreateAnalysis() =>
        AiAnalysis.Create(Guid.NewGuid(), Guid.NewGuid(), AiProvider.Gemini, "gemini-2.0", "v1.0", Now);

    [Fact]
    public void Create_SetsStatusPendingAndIsCurrentFalse()
    {
        var analysis = CreateAnalysis();

        analysis.Status.Should().Be(AiAnalysisStatus.Pending);
        analysis.IsCurrent.Should().BeFalse();
    }

    [Fact]
    public void Start_FromPending_TransitionsToRunning()
    {
        var analysis = CreateAnalysis();

        analysis.Start();

        analysis.Status.Should().Be(AiAnalysisStatus.Running);
    }

    [Fact]
    public void Start_WhenAlreadyRunning_ThrowsDomainException()
    {
        var analysis = CreateAnalysis();
        analysis.Start();

        var act = () => analysis.Start();

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("AI_ANALYSIS_INVALID_TRANSITION");
    }

    [Fact]
    public void Succeed_WhenNotRunning_ThrowsDomainException()
    {
        var analysis = CreateAnalysis();

        var act = () => analysis.Succeed("{}", "xulosa", "portret", Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("AI_ANALYSIS_INVALID_TRANSITION");
    }

    [Fact]
    public void Succeed_FromRunning_SetsCurrentAndRaisesEvent()
    {
        var analysis = CreateAnalysis();
        analysis.Start();

        analysis.Succeed("{}", "xulosa", "portret", Now.AddSeconds(5));

        analysis.Status.Should().Be(AiAnalysisStatus.Succeeded);
        analysis.IsCurrent.Should().BeTrue();
        analysis.DomainEvents.Should().ContainSingle(e => e is AiAnalysisSucceededEvent);
    }

    [Fact]
    public void Fail_FromRunning_SetsErrorAndRaisesEvent()
    {
        var analysis = CreateAnalysis();
        analysis.Start();

        analysis.Fail("Timeout", Now.AddSeconds(30));

        analysis.Status.Should().Be(AiAnalysisStatus.Failed);
        analysis.ErrorMessage.Should().Be("Timeout");
        analysis.DomainEvents.Should().ContainSingle(e => e is AiAnalysisFailedEvent);
    }

    [Fact]
    public void Fail_WhenNotRunning_ThrowsDomainException()
    {
        var analysis = CreateAnalysis();

        var act = () => analysis.Fail("Timeout", Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("AI_ANALYSIS_INVALID_TRANSITION");
    }

    [Fact]
    public void MarkNotCurrent_SetsIsCurrentFalse()
    {
        var analysis = CreateAnalysis();
        analysis.Start();
        analysis.Succeed("{}", "xulosa", "portret", Now);

        analysis.MarkNotCurrent();

        analysis.IsCurrent.Should().BeFalse();
    }
}
