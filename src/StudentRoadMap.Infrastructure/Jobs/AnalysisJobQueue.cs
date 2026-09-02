using Microsoft.Extensions.Logging;
using StudentRoadMap.Application.Common.Exceptions;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Jobs;

namespace StudentRoadMap.Infrastructure.Jobs;

/// <summary>
/// `IBackgroundJobQueue`ning haqiqiy (P18) implementatsiyasi — `analysis_jobs` jadvaliga
/// `Pending` yozuv qo'shadi; `AnalysisWorkerBackgroundService` uni keyinroq oladi
/// (`docs/09-ai-analiz-moduli.md` 8-bo'lim, `prompts/18` vazifa #1).
/// <para>
/// **Dublikat himoyasi** (`prompts/18` Cheklovlar: "Bir sessiya uchun bir vaqtda bitta job") —
/// DB darajasida `ux_analysis_jobs_active_per_assessment` filtrlangan unique indeksi
/// (`status IN (Pending, Running)`) orqali. Bu yerda ikki marta tekshirilmaydi (poyga holati
/// bo'lardi) — oddiy `INSERT` qilinadi, unique cheklov buzilsa `AppDbContext.SaveChangesAsync`
/// buni `UniqueConstraintViolationException`ga aylantiradi va biz jimgina (log bilan) yutamiz —
/// bu "allaqachon navbatda" degani, xato emas.
/// </para>
/// </summary>
public sealed class AnalysisJobQueue : IBackgroundJobQueue
{
    private readonly IAppDbContext _context;
    private readonly IDateTime _dateTime;
    private readonly ILogger<AnalysisJobQueue> _logger;

    public AnalysisJobQueue(IAppDbContext context, IDateTime dateTime, ILogger<AnalysisJobQueue> logger)
    {
        _context = context;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task EnqueueAiAnalysisAsync(
        Guid assessmentId,
        AiProvider? provider = null,
        string? promptVersion = null,
        CancellationToken cancellationToken = default)
    {
        var now = _dateTime.UtcNow;
        var job = AnalysisJob.Create(Guid.NewGuid(), assessmentId, now, provider, promptVersion);
        _context.Add(job);

        try
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("AI tahlil vazifasi navbatga qo'yildi: {AssessmentId}", assessmentId);
        }
        catch (UniqueConstraintViolationException)
        {
            // `ux_analysis_jobs_active_per_assessment` — shu sessiya uchun allaqachon
            // Pending/Running vazifa bor. Dublikat emas, kutilgan holat — xato emas.
            _logger.LogDebug("AI tahlil vazifasi allaqachon navbatda: {AssessmentId}", assessmentId);
        }
    }
}
