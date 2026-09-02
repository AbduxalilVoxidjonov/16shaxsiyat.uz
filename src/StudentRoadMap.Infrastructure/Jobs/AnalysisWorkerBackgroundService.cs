using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Jobs;

namespace StudentRoadMap.Infrastructure.Jobs;

/// <summary>
/// `analysis_jobs` navbatini poll qiladigan fon xizmati — `docs/09-ai-analiz-moduli.md`
/// 8-bo'lim ("Parallellik: bir vaqtda 4 ta job"), `prompts/18` vazifa #1.
/// <para>
/// **Bitta jarayon ichida ishlaydi** (MVP, `docs/13-deploy-va-infratuzilma.md` — bitta API
/// konteyneri): har poll-tsiklda BITTA `DbContext` bilan navbatdagi (`Pending`) vazifalarni
/// (max 4 tasi) ketma-ket "band" qiladi (`AnalysisJob.Start` → `SaveChangesAsync`) — poll-tsikl
/// yagona bo'lgani uchun bu yerda poyga holati YO'Q. Keyin har biri ALOHIDA DI scope'da
/// (alohida `DbContext`) PARALLEL ishga tushiriladi (`Task.WhenAll`). Gorizontal masshtablash
/// (bir nechta API instansi) uchun bu klass kengaytirilishi kerak (masalan
/// `SELECT ... FOR UPDATE SKIP LOCKED`/optimistik konkurentlik) — PM'ga hisobotda qayd etilgan.
/// </para>
/// </summary>
public sealed class AnalysisWorkerBackgroundService : BackgroundService
{
    /// <summary>`docs/09` 8-bo'lim: "bir vaqtda 4 ta job".</summary>
    private const int MaxParallelJobs = 4;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ErrorBackoff = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnalysisWorkerBackgroundService> _logger;

    public AnalysisWorkerBackgroundService(IServiceScopeFactory scopeFactory, ILogger<AnalysisWorkerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan waitBeforeNextPoll;
            try
            {
                var claimed = await ClaimPendingJobsAsync(stoppingToken).ConfigureAwait(false);

                if (claimed.Count > 0)
                {
                    await Task.WhenAll(claimed.Select(job => RunJobAsync(job.Id, job.AssessmentId, job.RequestedProvider, job.RequestedPromptVersion, stoppingToken)))
                        .ConfigureAwait(false);
                }

                waitBeforeNextPoll = PollInterval;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Poll-tsiklning o'zi (masalan DB vaqtincha yo'q) yiqilsa — butun xizmat
                // to'xtamasin, faqat log va uzunroq kutish bilan qayta urinadi.
                _logger.LogError(ex, "AnalysisWorkerBackgroundService: poll-tsiklda kutilmagan xato");
                waitBeforeNextPoll = ErrorBackoff;
            }

            try
            {
                await Task.Delay(waitBeforeNextPoll, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>`Pending` vazifalarni (eng eskisidan boshlab, max <see cref="MaxParallelJobs"/> ta) `Running`ga o'tkazadi va qaytaradi.</summary>
    private async Task<IReadOnlyList<AnalysisJob>> ClaimPendingJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var executor = scope.ServiceProvider.GetRequiredService<IAsyncQueryExecutor>();
        var dateTime = scope.ServiceProvider.GetRequiredService<IDateTime>();

        var pending = await executor.ToListAsync(
            context.AnalysisJobs.Where(j => j.Status == AnalysisJobStatus.Pending).OrderBy(j => j.CreatedAt).Take(MaxParallelJobs),
            cancellationToken).ConfigureAwait(false);

        if (pending.Count == 0)
        {
            return [];
        }

        var now = dateTime.UtcNow;
        foreach (var job in pending)
        {
            job.Start(now);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return pending;
    }

    /// <summary>Bitta vazifani ALOHIDA DI scope'da ishga tushiradi va yakunda `Completed`/`Failed` deb belgilaydi.</summary>
    private async Task RunJobAsync(
        Guid jobId,
        Guid assessmentId,
        Domain.Ai.AiProvider? requestedProvider,
        string? requestedPromptVersion,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IAnalysisOrchestrator>();

        try
        {
            await orchestrator.RunAsync(assessmentId, requestedProvider, requestedPromptVersion, cancellationToken).ConfigureAwait(false);
            await MarkJobAsync(jobId, succeeded: true, error: null, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Orkestratorning o'zi kutilmagan istisno bilan yiqilishi (masalan DB ulanishi
            // uzilishi) — AI provayder xatolari BU YERGA yetib kelmaydi (ular orkestrator
            // ichida `AiAnalysis.Fail`ga yozilib, `RunAsync` baribir muvaffaqiyatli qaytadi).
            _logger.LogError(ex, "AnalysisWorkerBackgroundService: vazifa yiqildi {JobId}/{AssessmentId}", jobId, assessmentId);
            await MarkJobAsync(jobId, succeeded: false, error: ex.Message, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task MarkJobAsync(Guid jobId, bool succeeded, string? error, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var executor = scope.ServiceProvider.GetRequiredService<IAsyncQueryExecutor>();
        var dateTime = scope.ServiceProvider.GetRequiredService<IDateTime>();

        var job = await executor.FirstOrDefaultAsync(context.AnalysisJobs.Where(j => j.Id == jobId), cancellationToken).ConfigureAwait(false);
        if (job is null)
        {
            return;
        }

        var now = dateTime.UtcNow;
        if (succeeded)
        {
            job.Complete(now);
        }
        else
        {
            job.Fail(error ?? "Noma'lum xato.", now);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
