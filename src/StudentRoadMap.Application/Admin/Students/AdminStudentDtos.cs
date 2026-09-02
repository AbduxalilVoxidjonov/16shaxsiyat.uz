namespace StudentRoadMap.Application.Admin.Students;

/// <summary>
/// `GET /api/admin/students` ro'yxat elementi — `docs/07-api-shartnoma.md` 3.2-bo'lim:
/// `id, fullName, schoolName, grade, classLetter, phone, lastAssessmentStatus, personalityType,
/// maturityIndex, activityLevel, needsAttention, reliabilityFlag, lastAssessmentAt`.
///
/// `LastAssessmentStatus`/`ReliabilityFlag` — `Student` SNAPSHOT ustunlarida YO'Q (`docs/05`
/// 2-bo'lim `students` DDL'ida bunday ustun yo'q, faqat `docs/07`ning misol javobida bor).
/// `ListStudentsQueryHandler` bularni SAHIFA hajmida (≤100 o'quvchi) alohida, qimmat bo'lmagan
/// batch so'rov bilan `assessments`dan to'ldiradi — PM'ga savol: kelajakda bular ham
/// `students` snapshot ustunlariga ko'chirilsinmi (`docs/04`/`docs/05`ni yangilab)?
/// </summary>
public sealed record AdminStudentListItemDto(
    Guid Id,
    string FullName,
    string SchoolName,
    int Grade,
    string? ClassLetter,
    string Phone,
    string? LastAssessmentStatus,
    string? PersonalityType,
    double? MaturityIndex,
    string? ActivityLevel,
    bool NeedsAttention,
    string? ReliabilityFlag,
    DateTimeOffset? LastAssessmentAt);

/// <summary>`GET /api/admin/students/{id}` — `docs/07` 3.2-bo'lim `student` qismi.</summary>
public sealed record AdminStudentDetailDto(
    Guid Id,
    string FullName,
    DateOnly BirthDate,
    int Age,
    string Gender,
    int Grade,
    string? ClassLetter,
    string Phone,
    string? ParentPhone,
    string? Email,
    AdminStudentSchoolRefDto School,
    DateTimeOffset ConsentGivenAt,
    DateTimeOffset CreatedAt);

public sealed record AdminStudentSchoolRefDto(Guid Id, string Name);

/// <summary>`docs/07` 3.2-bo'lim `assessments[]` — sessiyalar ro'yxati (eng yangisi birinchi).</summary>
public sealed record AdminAssessmentSummaryDto(
    Guid Id,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int? DurationMinutes,
    double? ReliabilityScore,
    string? ReliabilityFlag,
    bool IsLatest);

/// <summary>`docs/07` 3.2-bo'lim to'liq javobi: `student`, `assessments`, `latestAssessment`.</summary>
public sealed record AdminStudentProfileDto(
    AdminStudentDetailDto Student,
    IReadOnlyList<AdminAssessmentSummaryDto> Assessments,
    AdminLatestAssessmentDto? LatestAssessment);

/// <summary>`docs/07` 3.2-bo'lim `latestAssessment` — `results`/`aiAnalysis`/`aiHistory`.</summary>
public sealed record AdminLatestAssessmentDto(
    Guid Id,
    AdminTestResultsDto Results,
    AdminAiAnalysisDto? AiAnalysis,
    IReadOnlyList<AdminAiHistoryItemDto> AiHistory);

/// <summary>Har test — mavjud bo'lsagina to'ldiriladi (o'quvchi hali yechmagan test `null`).</summary>
public sealed record AdminTestResultsDto(
    AdminMbti16Dto? Mbti16,
    AdminBig5Dto? Big5,
    AdminRiasecDto? Riasec,
    AdminActivityDto? Activity);

public sealed record AdminAxisDto(double Pct, string Letter, bool Borderline);

/// <summary>`docs/07` 3.2: `{resultCode, typeName, axes, borderlineAxes}`. `scale`/`scaleDirection` YO'Q (`CLAUDE.md` 9-band).</summary>
public sealed record AdminMbti16Dto(
    string ResultCode,
    string TypeName,
    IReadOnlyDictionary<string, AdminAxisDto> Axes,
    IReadOnlyList<string> BorderlineAxes);

public sealed record AdminFactorDto(double Raw, double Pct, string Level);

/// <summary>`docs/07` 3.2: `{factors, stabilityPct, maturityIndex, maturityLevel}`.</summary>
public sealed record AdminBig5Dto(
    IReadOnlyDictionary<string, AdminFactorDto> Factors,
    double StabilityPct,
    double? MaturityIndex,
    string? MaturityLevel);

public sealed record AdminCareerFieldDto(string Name, IReadOnlyList<string> Professions);

/// <summary>`docs/07` 3.2: `{resultCode, types, differentiation, consistency, careerFields}`.</summary>
public sealed record AdminRiasecDto(
    string ResultCode,
    IReadOnlyDictionary<string, double> Types,
    double Differentiation,
    string Consistency,
    IReadOnlyList<AdminCareerFieldDto> CareerFields);

/// <summary>`docs/07` 3.2: `{scales, activityIndex, activityLevel, needsAttention}`.</summary>
public sealed record AdminActivityDto(
    IReadOnlyDictionary<string, double> Scales,
    double? ActivityIndex,
    string? ActivityLevel,
    bool NeedsAttention);

/// <summary>
/// `docs/07` 3.2 `aiAnalysis` — P16-P18 (AI modul) tugagunga qadar BO'SH bo'lishi mumkin
/// (`prompts/14` Vazifa 2-band). Faqat `AiAnalysis` entity'sida HAQIQATDA mavjud maydonlar
/// to'ldiriladi (`learningStyle`/`motivationProfile`/`activityAssessment`/`disclaimer` —
/// `docs/07` misolida bor, lekin `docs/04` 2.8-bo'lim entity'sida bunday maydon yo'q,
/// ehtimol `ResponseJson`dan P16-18da qo'shiladi — PM'ga savol).
/// </summary>
public sealed record AdminAiAnalysisDto(
    Guid Id,
    string Status,
    string Provider,
    string Model,
    string PromptVersion,
    DateTimeOffset CreatedAt,
    string? Summary,
    string? PersonalityPortrait,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> GrowthAreas,
    IReadOnlyList<AdminCareerSuggestionDto> CareerSuggestions,
    IReadOnlyList<string> StudentRecommendations,
    string? TeacherNotes,
    string? ParentNotes,
    IReadOnlyList<string> AttentionFlags);

public sealed record AdminCareerSuggestionDto(string Field, string Why, IReadOnlyList<string> NextSteps);

public sealed record AdminAiHistoryItemDto(Guid Id, string Provider, DateTimeOffset CreatedAt, string Status, bool IsCurrent);
