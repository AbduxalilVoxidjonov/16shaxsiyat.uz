namespace StudentRoadMap.Application.Admin.Assessments;

/// <summary>
/// `GET /api/admin/assessments` ro'yxat elementi — `docs/07-api-shartnoma.md` 3.3-bo'lim
/// jadvalida aniq JSON namunasi YO'Q (faqat yo'l/filtr jadvali). Shakl `AdminStudentListItemDto`
/// (3.2-bo'lim, mavjud namuna) uslubida tanlandi: sessiyani ro'yxatda tanib olish uchun kerakli
/// minimal maydonlar + o'quvchi/maktab nomi (JOIN'siz emas — sahifa hajmida batch, pastga qarang).
/// PM'ga savol: bu shakl `docs/07`ga rasman kiritilsinmi?
/// </summary>
public sealed record AdminAssessmentListItemDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid SchoolId,
    string SchoolName,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int? DurationMinutes,
    double? ReliabilityScore,
    string? ReliabilityFlag);

/// <summary>
/// `GET /api/admin/assessments/{id}/answers` — `docs/07` 3.3-bo'lim: "Xom javoblar (audit
/// uchun)". `prompts/15` MAXSUS DIQQAT #5: savol matni, javob, `durationMs`, `revisionCount` —
/// `scale`/`scaleDirection` BU YERDA HAM YO'Q (`CLAUDE.md` 9-band ruhi — ular faqat admin
/// KATALOGIDA ko'rinadi, javoblar ro'yxatida emas). Bog'langan (max ~190 savol/sessiya,
/// `docs/05` §5) — sahifalanmaydi, to'liq ro'yxat qaytadi.
/// </summary>
public sealed record AdminAssessmentAnswerDto(
    Guid QuestionId,
    string QuestionCode,
    string TestCode,
    string QuestionText,
    int RawValue,
    string? SelectedOptionText,
    int DurationMs,
    int RevisionCount,
    DateTimeOffset AnsweredAt);

/// <summary>
/// `POST /api/admin/assessments/{id}/recalculate-scores` javobi — `docs/07`da aniq namuna YO'Q.
/// Yangilangan natijalarni (`AdminTestResultsDto` — `Admin.Students`dagi bilan BIR XIL shakl,
/// `CLAUDE.md` 9-band) va ishonchlilikni qaytaradi; `Changed` — kamida bitta qiymat
/// (natija/ishonchlilik) eski qiymatdan farq qilganini bildiradi (audit/UI uchun qulaylik).
/// </summary>
public sealed record AdminRecalculateScoresResultDto(
    Guid AssessmentId,
    StudentRoadMap.Application.Admin.Students.AdminTestResultsDto Results,
    double? ReliabilityScore,
    string? ReliabilityFlag,
    bool Changed);
