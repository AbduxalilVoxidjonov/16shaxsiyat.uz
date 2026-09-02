namespace StudentRoadMap.Application.Admin.Dashboard;

/// <summary>`docs/07-api-shartnoma.md` 3.6-bo'lim `totals` — hamma vaqt bo'yicha (oynaga bog'liq emas).</summary>
public sealed record AdminDashboardTotalsDto(
    int Schools,
    int ActiveSchools,
    int Students,
    int CompletedAssessments,
    int PendingAnalysis,
    int NeedsAttention);

/// <summary>
/// `docs/07` 3.6-bo'lim `last30Days` — `?from=&to=` berilsa o'sha oraliqqa mos (`prompts/15`da
/// aniq ko'rsatilmagan, PM'ga savol: standart oyna 30 kunmi doim shundaymi?), bo'lmasa
/// standart oxirgi 30 kun (`now - 30d .. now`). Maydon nomi hujjatdagi kabi qoladi
/// ("javob shakllari aynan") — oyna uzunligi 30 kundan farq qilsa ham.
/// </summary>
public sealed record AdminDashboardLast30DaysDto(
    int NewStudents,
    int Completed,
    double AvgDurationMinutes,
    double AvgReliability,
    double DropOffRate);

public sealed record AdminDashboardPersonalityItemDto(string Type, int Count);

public sealed record AdminDashboardActivityItemDto(string Level, int Count);

public sealed record AdminDashboardHollandItemDto(string Code, int Count);

public sealed record AdminDashboardRecentAssessmentDto(
    Guid AssessmentId,
    string StudentName,
    string SchoolName,
    DateTimeOffset CompletedAt,
    string Status);

/// <summary>`GET /api/admin/dashboard/stats?from=&to=` — `docs/07` 3.6-bo'lim to'liq javobi.</summary>
public sealed record AdminDashboardStatsDto(
    AdminDashboardTotalsDto Totals,
    AdminDashboardLast30DaysDto Last30Days,
    IReadOnlyList<AdminDashboardPersonalityItemDto> PersonalityDistribution,
    IReadOnlyList<AdminDashboardActivityItemDto> ActivityDistribution,
    IReadOnlyList<AdminDashboardHollandItemDto> HollandTop,
    IReadOnlyList<AdminDashboardRecentAssessmentDto> RecentAssessments);
