namespace StudentRoadMap.Application.PublicUsers.ListAssessments;

/// <summary>`docs/07` 5.2-bo'lim javob shakli.</summary>
public sealed record ListMyAssessmentsResult(IReadOnlyList<MyAssessmentDto> Items);

/// <summary>
/// Kabinet ro'yxatidagi bitta sessiya. `id` BOR — bu `CLAUDE.md` 8-qoidasini BUZMAYDI:
/// qoida "ommaviy API'da ID egalikni aniqlamasin" degani, bu yerda esa egalik JWT bilan
/// isbotlangan va ro'yxatning O'ZI faqat shu foydalanuvchining sessiyalaridan iborat.
/// Boshqa foydalanuvchining `id`si bilan `/result` so'ralsa `404` qaytadi (mavjudligini
/// oshkor qilmaslik uchun `403` emas).
///
/// Ball/indeks/bayroq maydonlari ATAYLAB yo'q — kabinet ro'yxati faqat navigatsiya uchun
/// (`GetStudentResultResult` bilan bir xil cheklov, `prompts/12`).
/// </summary>
public sealed record MyAssessmentDto(
    Guid Id,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string ProgramCode,
    string ProgramName,
    /// <summary>Natija hozir ochilishi mumkinmi (`Analyzed` + makon bayrog'i) — frontend tugmani shu bo'yicha ko'rsatadi.</summary>
    bool ResultAvailable);
