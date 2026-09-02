using FluentAssertions;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Jobs;

namespace StudentRoadMap.Domain.Tests.Jobs;

/// <summary>`AnalysisJob` holat mashinasi — P18 (`prompts/18`), `docs/09-ai-analiz-moduli.md` 8-bo'lim.</summary>
public sealed class AnalysisJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_SetsStatusPendingAndZeroAttempts()
    {
        var job = AnalysisJob.Create(Guid.NewGuid(), Guid.NewGuid(), Now);

        job.Status.Should().Be(AnalysisJobStatus.Pending);
        job.AttemptCount.Should().Be(0);
        job.RequestedProvider.Should().BeNull();
        job.RequestedPromptVersion.Should().BeNull();
    }

    [Fact]
    public void Start_FromPending_TransitionsToRunningAndIncrementsAttemptCount()
    {
        var job = AnalysisJob.Create(Guid.NewGuid(), Guid.NewGuid(), Now);

        job.Start(Now.AddSeconds(2));

        job.Status.Should().Be(AnalysisJobStatus.Running);
        job.AttemptCount.Should().Be(1);
    }

    [Fact]
    public void Start_FromRunning_ThrowsDomainException()
    {
        var job = AnalysisJob.Create(Guid.NewGuid(), Guid.NewGuid(), Now);
        job.Start(Now);

        var act = () => job.Start(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("ANALYSIS_JOB_INVALID_TRANSITION");
    }

    [Fact]
    public void Start_FromFailed_AllowsRetry()
    {
        var job = AnalysisJob.Create(Guid.NewGuid(), Guid.NewGuid(), Now);
        job.Start(Now);
        job.Fail("Ulanish uzildi.", Now);

        job.Start(Now.AddMinutes(1));

        job.Status.Should().Be(AnalysisJobStatus.Running);
        job.AttemptCount.Should().Be(2);
    }

    [Fact]
    public void Complete_FromRunning_TransitionsToCompleted()
    {
        var job = AnalysisJob.Create(Guid.NewGuid(), Guid.NewGuid(), Now);
        job.Start(Now);

        job.Complete(Now.AddSeconds(5));

        job.Status.Should().Be(AnalysisJobStatus.Completed);
    }

    [Fact]
    public void Complete_WhenNotRunning_ThrowsDomainException()
    {
        var job = AnalysisJob.Create(Guid.NewGuid(), Guid.NewGuid(), Now);

        var act = () => job.Complete(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("ANALYSIS_JOB_INVALID_TRANSITION");
    }

    [Fact]
    public void Fail_FromRunning_SetsErrorAndStatus()
    {
        var job = AnalysisJob.Create(Guid.NewGuid(), Guid.NewGuid(), Now);
        job.Start(Now);

        job.Fail("DB ulanishi uzildi.", Now.AddSeconds(1));

        job.Status.Should().Be(AnalysisJobStatus.Failed);
        job.LastError.Should().Be("DB ulanishi uzildi.");
    }

    [Fact]
    public void Create_WithRequestedProviderAndPromptVersion_PreservesThem()
    {
        var job = AnalysisJob.Create(Guid.NewGuid(), Guid.NewGuid(), Now, StudentRoadMap.Domain.Ai.AiProvider.Anthropic, "v1.1");

        job.RequestedProvider.Should().Be(StudentRoadMap.Domain.Ai.AiProvider.Anthropic);
        job.RequestedPromptVersion.Should().Be("v1.1");
    }

    [Fact]
    public void Create_EmptyAssessmentId_ThrowsArgumentException()
    {
        var act = () => AnalysisJob.Create(Guid.NewGuid(), Guid.Empty, Now);

        act.Should().Throw<ArgumentException>();
    }
}
