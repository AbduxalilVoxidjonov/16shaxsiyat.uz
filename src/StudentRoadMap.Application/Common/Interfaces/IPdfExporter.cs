using StudentRoadMap.Application.Admin.Students;

namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// Individual profil PDF hisoboti generatsiyasi abstraksiyasi (`prompts/27-eksport-excel-va-pdf.md`)
/// — `Infrastructure/Export/PdfExporter` `QuestPDF` orqali amalga oshiradi (`docs/06`da
/// ko'rsatilgan kutubxona). `Application` qatlami `QuestPDF` paketiga bog'lanmaydi
/// (`docs/06-arxitektura.md` 3-bo'lim).
/// </summary>
public interface IPdfExporter
{
    /// <summary>
    /// Sinxron — `QuestPDF` generatsiyasi CPU-bog'liq (I/O kutish yo'q), `docs/09` uslubidagi
    /// "Task.Run bilan o'rash" shart emas (`prompts/27` cheklovi: 10 soniyadan oshmasin —
    /// oddiy matn/jadval/SVG diagramma bilan bunga yetarlicha zahira bor).
    /// </summary>
    byte[] GenerateAssessmentReport(AssessmentReportData data);
}

/// <summary>
/// PDF hisobot uchun kerakli hamma ma'lumot — bitta yassi (flat) DTO, `Infrastructure`
/// qatlami buni layout'ga aylantiradi. Shaxsiy ma'lumot (FISH, sinf) shu yerda BOR — bu
/// ADMIN hisoboti (`CLAUDE.md` 5-band faqat AI PROVAYDERGA yuborishni cheklaydi, admin PDF'ga
/// emas). `Results`/`AiAnalysis` — `StudentProfileMapping` allaqachon qurgan DTO'lar qayta
/// ishlatiladi (`scale`/`scaleDirection` ularda YO'Q, `CLAUDE.md` 9-band).
/// </summary>
public sealed record AssessmentReportData(
    Guid AssessmentId,
    string StudentFullName,
    string SchoolName,
    int Grade,
    string? ClassLetter,
    /// <summary>P52: anonim o'quvchida (`IsAnonymous`) `null` — tug'ilgan sana yo'q.</summary>
    int? Age,
    string Gender,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    double? ReliabilityScore,
    string? ReliabilityFlag,
    AdminTestResultsDto Results,
    AdminAiAnalysisDto? AiAnalysis,
    DateTimeOffset GeneratedAt);
