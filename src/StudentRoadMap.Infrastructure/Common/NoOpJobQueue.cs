using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Infrastructure.Common;

/// <summary>
/// `IBackgroundJobQueue`ning vaqtinchalik (P12) implementatsiyasi — hech narsa qilmaydi,
/// faqat kontraktni bajaradi (`prompts/12`: "implementatsiyasi hozircha bo'sh — `NoOpJobQueue`").
/// P18 dan boshlab `DependencyInjection.AddInfrastructure` bu klass o'rniga
/// `Infrastructure/Jobs/AnalysisJobQueue`ni ro'yxatdan o'tkazadi — bu klass endi ishlatilmaydi,
/// lekin (`CLAUDE.md` "FAYL O'CHIRMA" qoidasi) saqlab qolingan.
/// </summary>
internal sealed class NoOpJobQueue : IBackgroundJobQueue
{
    public Task EnqueueAiAnalysisAsync(
        Guid assessmentId,
        AiProvider? provider = null,
        string? promptVersion = null,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
