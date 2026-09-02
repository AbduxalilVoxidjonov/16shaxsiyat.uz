using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Jobs;
using StudentRoadMap.Infrastructure.Jobs;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Tests.Jobs.Testing;
using StudentRoadMap.Infrastructure.Tests.Testing;

namespace StudentRoadMap.Infrastructure.Tests.Jobs;

/// <summary>
/// `AnalysisJobQueue` — `prompts/18` Cheklovlar: "Bir sessiya uchun bir vaqtda bitta job
/// (dublikat navbatga qo'yish bloklanadi)".
/// </summary>
public sealed class AnalysisJobQueueTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);

    private static AppDbContext NewContext(SqliteConnection connection) =>
        SqliteAppDbContextFactory.CreateContext(connection, new FixedDateTimeProvider(Now), new NoOpPublisher());

    [Fact]
    public async Task EnqueueAiAnalysisAsync_InsertsPendingJob()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();

        var assessment = TestEntityFactory.CreateDraftAssessment(context, Now);
        await context.SaveChangesAsync();

        var queue = new AnalysisJobQueue(context, new FixedDateTimeProvider(Now), NullLogger<AnalysisJobQueue>.Instance);

        await queue.EnqueueAiAnalysisAsync(assessment.Id);

        var job = context.AnalysisJobs.Single(j => j.AssessmentId == assessment.Id);
        job.Status.Should().Be(AnalysisJobStatus.Pending);
        job.RequestedProvider.Should().BeNull();
        job.RequestedPromptVersion.Should().BeNull();
    }

    [Fact]
    public async Task EnqueueAiAnalysisAsync_WithProviderAndPromptVersion_PersistsThem()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();

        var assessment = TestEntityFactory.CreateDraftAssessment(context, Now);
        await context.SaveChangesAsync();

        var queue = new AnalysisJobQueue(context, new FixedDateTimeProvider(Now), NullLogger<AnalysisJobQueue>.Instance);

        await queue.EnqueueAiAnalysisAsync(assessment.Id, AiProvider.Anthropic, "v1.1");

        var job = context.AnalysisJobs.Single(j => j.AssessmentId == assessment.Id);
        job.RequestedProvider.Should().Be(AiProvider.Anthropic);
        job.RequestedPromptVersion.Should().Be("v1.1");
    }

    [Fact]
    public async Task EnqueueAiAnalysisAsync_CalledTwiceForSameAssessment_DoesNotCreateDuplicatePendingJob()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();

        var assessment = TestEntityFactory.CreateDraftAssessment(context, Now);
        await context.SaveChangesAsync();

        var queue = new AnalysisJobQueue(context, new FixedDateTimeProvider(Now), NullLogger<AnalysisJobQueue>.Instance);

        await queue.EnqueueAiAnalysisAsync(assessment.Id);
        await queue.EnqueueAiAnalysisAsync(assessment.Id);

        context.AnalysisJobs.Count(j => j.AssessmentId == assessment.Id).Should().Be(1);
    }

    [Fact]
    public async Task EnqueueAiAnalysisAsync_AfterPreviousJobCompleted_AllowsNewJob()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();

        var assessment = TestEntityFactory.CreateDraftAssessment(context, Now);
        await context.SaveChangesAsync();

        var queue = new AnalysisJobQueue(context, new FixedDateTimeProvider(Now), NullLogger<AnalysisJobQueue>.Instance);

        await queue.EnqueueAiAnalysisAsync(assessment.Id);
        var firstJob = context.AnalysisJobs.Single(j => j.AssessmentId == assessment.Id);
        firstJob.Start(Now);
        firstJob.Complete(Now.AddSeconds(5));
        await context.SaveChangesAsync();

        // Rerun ssenariysi: avvalgi vazifa `Completed` — yangisi to'sqinliksiz qo'shilishi kerak
        // (unique indeks faqat `Pending`/`Running`ni cheklaydi).
        await queue.EnqueueAiAnalysisAsync(assessment.Id);

        context.AnalysisJobs.Count(j => j.AssessmentId == assessment.Id).Should().Be(2);
    }
}
