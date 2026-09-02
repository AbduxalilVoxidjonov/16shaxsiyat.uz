using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Infrastructure.Jobs;

/// <summary>
/// `docs/09-ai-analiz-moduli.md` 1-bo'lim oqimini bajaradi: `PromptBuilder` → `IAiProviderResolver`
/// → provider → `AiResponseValidator` → `AiAnalysis`. `AnalysisWorkerBackgroundService` (haqiqiy
/// navbatdan) va `AnalysisJobQueue.EnqueueAiAnalysisAsync` chaqirgan har bir vazifa uchun
/// ALOHIDA DI scope'da ishga tushiriladi (P18, `prompts/18` vazifa #2).
/// </summary>
public interface IAnalysisOrchestrator
{
    /// <summary>
    /// `assessmentId` — tahlil qilinadigan sessiya. `requestedProvider`/`requestedPromptVersion` —
    /// `RerunAnalysisCommand`da aniq berilgan bo'lsa (aks holda `null`: standart provider,
    /// faol prompt versiyasi).
    /// <para>
    /// **Idempotent**: `Assessment.Status != Analyzing` bo'lsa (masalan boshqa ishchi allaqachon
    /// yakunlagan, yoki hech qachon navbatga qo'yilmagan) — hech narsa qilmasdan chiqadi
    /// (`prompts/18` vazifa #2: "idempotent: job boshida holat tekshiriladi").
    /// </para>
    /// </summary>
    Task RunAsync(Guid assessmentId, AiProvider? requestedProvider, string? requestedPromptVersion, CancellationToken cancellationToken);
}
