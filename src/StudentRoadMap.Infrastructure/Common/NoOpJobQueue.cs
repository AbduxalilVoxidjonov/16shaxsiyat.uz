using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Common;

/// <summary>
/// `IBackgroundJobQueue`ning vaqtinchalik (P12) implementatsiyasi — hech narsa qilmaydi,
/// faqat kontraktni bajaradi (`prompts/12`: "implementatsiyasi hozircha bo'sh — `NoOpJobQueue`").
/// Haqiqiy AI navbati/worker P18 da shu klass o'rniga ulanadi.
/// </summary>
internal sealed class NoOpJobQueue : IBackgroundJobQueue
{
    public Task EnqueueAiAnalysisAsync(Guid assessmentId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
