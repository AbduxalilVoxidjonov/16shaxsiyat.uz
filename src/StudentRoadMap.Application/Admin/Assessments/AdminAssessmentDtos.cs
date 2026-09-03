namespace StudentRoadMap.Application.Admin.Assessments;

/// <summary>
/// `GET /api/admin/assessments` ro'yxat elementi — `docs/07-api-shartnoma.md` 3.3-bo'lim
/// jadvalida aniq JSON namunasi YO'Q (faqat yo'l/filtr jadvali). Shakl `AdminStudentListItemDto`
/// (3.2-bo'lim, mavjud namuna) uslubida tanlandi: sessiyani ro'yxatda tanib olish uchun kerakli
/// minimal maydonlar + o'quvchi/maktab nomi (JOIN'siz emas — sahifa hajmida batch, pastga qarang).
/// Shakl `docs/07` 3.3-bo'limiga rasman kiritildi (2026-09-03).
///
/// <para>
/// **`ProgramId`/`ProgramName`** (2026-09-03): `Assessment.ProgramId` domenda MAJBURIY
/// (`docs/06` 8-bo'lim, 2026-09-02 qaror), lekin ro'yxat javobiga chiqarilmagan edi — admin
/// jadvalidagi "Dastur" ustuni doim "—" ko'rsatardi. `ProgramName` — `AssessmentProgram.NameUz`;
/// dastur yozuvi topilmasa `null` (bo'sh satr yoki soxta nom EMAS).
/// </para>
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
    string? ReliabilityFlag,
    Guid ProgramId,
    string? ProgramName);

/// <summary>
/// `GET /api/admin/assessments/{id}` javobi — `docs/07` 3.3-bo'lim.
///
/// <para>
/// **Birinchi to'rt maydon** (`id`, `results`, `aiAnalysis`, `aiHistory`) — `AdminLatestAssessmentDto`
/// (3.2-bo'lim `latestAssessment`) bilan HARFMA-HARF bir xil, shu jumladan `results` kalitlari
/// (`MBTI16`/`BIG5`/`RIASEC`/`ACTIVITY`, `AdminTestResultsDto`dagi `JsonPropertyName` bilan
/// qulflangan, 2026-09-03 qarori). Ular SHARTNOMA — o'zgartirilmaydi.
/// </para>
/// <para>
/// **Qolgan maydonlar — sessiya "sarlavhasi"** (2026-09-03): avval detal javobida sessiyaning
/// o'zi haqida HECH NARSA yo'q edi (holat, vaqtlar, ishonchlilik, o'quvchi, maktab, dastur), shu
/// sabab admin sahifasi ularni ro'yxatdan navigatsiya holati (`Link state`) orqali olardi —
/// havolani NUSXALAB ochilganda sahifa yarim bo'sh qolardi. Endi detalning o'zi to'liq.
/// </para>
/// <para>
/// **Ma'lumot yo'q ≠ nol** (`docs/06` qarorlar jurnali, 2026-09-02): yakunlanmagan sessiyada
/// `CompletedAt`/`DurationMinutes` — `null`; ishonchlilik hisoblanmagan bo'lsa
/// `ReliabilityScore`/`ReliabilityFlag` — `null`. `Student`/`School`/`Program` bog'liq yozuv
/// topilmasa (masalan o'quvchi soft-delete qilingan) `null` — bo'sh satrli soxta obyekt EMAS.
/// </para>
/// </summary>
public sealed record AdminAssessmentDetailDto(
    Guid Id,
    StudentRoadMap.Application.Admin.Students.AdminTestResultsDto Results,
    StudentRoadMap.Application.Admin.Students.AdminAiAnalysisDto? AiAnalysis,
    IReadOnlyList<StudentRoadMap.Application.Admin.Students.AdminAiHistoryItemDto> AiHistory,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int? DurationMinutes,
    double? ReliabilityScore,
    string? ReliabilityFlag,
    AdminAssessmentStudentRefDto? Student,
    AdminAssessmentSchoolRefDto? School,
    AdminAssessmentProgramRefDto? Program,
    IReadOnlyList<AdminAssessmentTestItemDto> Tests);

/// <summary>Sessiya egasi — `{id, fullName}` (`docs/07` 3.3).</summary>
public sealed record AdminAssessmentStudentRefDto(Guid Id, string FullName);

/// <summary>Sessiya maktabi — `{id, name}` (`docs/07` 3.3).</summary>
public sealed record AdminAssessmentSchoolRefDto(Guid Id, string Name);

/// <summary>Sessiya dasturi — `{id, nameUz}` (`docs/07` 3.3, `AssessmentProgram`).</summary>
public sealed record AdminAssessmentProgramRefDto(Guid Id, string NameUz);

/// <summary>
/// Sessiyadagi bitta test bloki — `AssessmentTest` + uning `TestDefinition` metama'lumoti
/// (`docs/07` 3.3, 2026-09-03). Bungacha detal javobi faqat 4 ta TIZIM blokini (`results`)
/// bilardi va dasturdagi `Custom`/`Survey` anketalar admin ekranida umuman ko'rinmasdi.
///
/// <para>
/// Maydon nomi `TestCode` (`Code` emas) — `AdminAssessmentAnswerDto.TestCode` va
/// `GET /answers?testCode=` bilan bir xil atama, mijoz tomonidagi tayyor tip ham shuni
/// kutadi (`frontend/src/features/assessments/model/types.ts` → `AssessmentTestItemDto`).
/// </para>
/// <para>
/// `ScoringMode` — `Scored` | `Survey` (`docs/06` 8-bo'lim, 2026-09-02): `Survey` anketa
/// BALLANMAYDI, shu sabab uning natijasi `results`da yo'q va UI uni "0 ball" emas,
/// "Ballanmaydi (so'rovnoma)" deb ko'rsatadi. `QuestionCount`/`AnsweredCount` —
/// `AssessmentTest.TotalCount`/`AnsweredCount`, ya'ni SESSIYA snapshoti (katalogdagi joriy
/// savol soni emas): keyin anketaga savol qo'shilsa ham eski sessiya progressi buzilmaydi.
/// `scale`/`scaleDirection` bu yerda ham YO'Q (`CLAUDE.md` 9-band).
/// </para>
/// </summary>
public sealed record AdminAssessmentTestItemDto(
    Guid TestDefinitionId,
    string TestCode,
    string NameUz,
    string ScoringMode,
    string Status,
    int QuestionCount,
    int AnsweredCount);

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
