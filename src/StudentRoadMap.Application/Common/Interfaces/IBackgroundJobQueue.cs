using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// AI tahlilini fon navbatiga qo'yish uchun abstraksiya (`prompts/12`: "Scoring sinxron, AI —
/// asinxron"). `Application` bu interfeysni e'lon qiladi; `CompleteSessionCommandHandler`
/// sessiya `Analyzing`ga o'tgach (`IPostCommitActions` orqali, P18-R1) va
/// `RerunAnalysisCommandHandler` qayta urinishda chaqiradi. Haqiqiy implementatsiya —
/// `Infrastructure/Jobs/AnalysisJobQueue` (P18, `analysis_jobs` jadvali +
/// `AnalysisWorkerBackgroundService`); `NoOpJobQueue` faqat testlar/eski kontrakt uchun qoldirilgan.
/// </summary>
public interface IBackgroundJobQueue
{
    /// <summary>
    /// Berilgan sessiya uchun AI tahlilini navbatga qo'yadi. `provider`/`promptVersion` —
    /// `RerunAnalysisCommand`da aniq so'ralgan bo'lsa (`docs/07` §3.3: "`{ provider?,
    /// promptVersion? }`"); oddiy oqimda (`CompleteSessionCommandHandler`) ikkalasi ham
    /// `null` — orkestrator standart (`IsDefault`) providerni va faol prompt versiyasini oladi.
    /// </summary>
    Task EnqueueAiAnalysisAsync(
        Guid assessmentId,
        AiProvider? provider = null,
        string? promptVersion = null,
        CancellationToken cancellationToken = default);
}
