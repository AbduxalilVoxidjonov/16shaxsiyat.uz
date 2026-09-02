namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// AI tahlilini fon navbatiga qo'yish uchun abstraksiya (`prompts/12`: "Scoring sinxron, AI —
/// asinxron"). `Application` bu interfeysni e'lon qiladi va `CompleteSessionCommandHandler`
/// sessiya `Analyzing`ga o'tganda chaqiradi — implementatsiyasi hozircha `Infrastructure`da
/// `NoOpJobQueue` (hech narsa qilmaydi, faqat kontraktni bajaradi). Haqiqiy navbat/worker
/// (`docs/06-arxitektura.md` "Fon ishlari: navbat + retry + fallback zanjiri") P18 da ulanadi.
/// </summary>
public interface IBackgroundJobQueue
{
    /// <summary>Berilgan sessiya uchun AI tahlilini navbatga qo'yadi.</summary>
    Task EnqueueAiAnalysisAsync(Guid assessmentId, CancellationToken cancellationToken = default);
}
