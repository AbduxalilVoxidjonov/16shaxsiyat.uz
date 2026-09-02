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
///
/// `AvgDurationMinutes`/`AvgReliability`/`DropOffRate` — **`double?`** (PM topilmasi,
/// 2026-09-02, `docs/06` §8 qoidasi bilan bir xil: "ma'lumot yo'q bo'lsa `null`, hech qachon
/// `0` emas"). Oynada yakunlangan/boshlangan sessiya UMUMAN YO'Q (`null`) bilan HAQIQIY `0`
/// qiymat (masalan 0% tashlab ketish) ajratiladi — aks holda dashboard "o'rtacha ishonchlilik
/// 0" deb ko'rsatib, adminni yolg'ondan xavotirga soladi (hali sessiya yo'qligini emas).
/// </summary>
public sealed record AdminDashboardLast30DaysDto(
    int NewStudents,
    int Completed,
    double? AvgDurationMinutes,
    double? AvgReliability,
    double? DropOffRate);

public sealed record AdminDashboardPersonalityItemDto(string Type, int Count);

public sealed record AdminDashboardActivityItemDto(string Level, int Count);

public sealed record AdminDashboardHollandItemDto(string Code, int Count);

public sealed record AdminDashboardRecentAssessmentDto(
    Guid AssessmentId,
    string StudentName,
    string SchoolName,
    DateTimeOffset CompletedAt,
    string Status);

/// <summary>
/// `docs/07` 3.6-bo'lim kengaytmasi (`prompts/15` vazifa 2, 2026-09-02) — umumiy analitika
/// voronkasi, `[from, to]` OYNASIGA bog'liq (`Assessment.StartedAt`/`Student.CreatedAt`/
/// `SchoolLinkView.DateUtc` shu oynada). Bosqichlar KETMA-KET TORAYIB boradigan qat'iy
/// pastki to'plamlar EMAS (masalan `linkViews` boshqa jadvaldan — ID bo'yicha bog'lanish yo'q,
/// faqat kunlik son), lekin har biri o'z aniq DB shartiga ega:
/// - `linkViews`  — `school_link_views.count` yig'indisi (oynadagi kunlar bo'yicha).
/// - `registered` — `Student.CreatedAt` oynada (yangi yaratilgan o'quvchilar, `last30Days.newStudents` bilan BIR XIL son).
/// - `started`    — `Assessment.StartedAt` oynada VA `Status != Draft` (kamida bitta test boshlangan).
/// - `completed`  — `Assessment.StartedAt` oynada VA `CompletedAt != null` (`last30Days.completed` bilan BIR XIL son).
/// - `analyzed`   — `Assessment.StartedAt` oynada VA `Status == Analyzed`.
/// </summary>
public sealed record AdminDashboardFunnelDto(
    int LinkViews,
    int Registered,
    int Started,
    int Completed,
    int Analyzed);

/// <summary>
/// Maktab kesimidagi voronka (`prompts/15` vazifa 2) — `[from, to]` OYNASIGA bog'liq, eng faoli
/// (`registered` bo'yicha, keyin `completed`, keyin `linkViews`) BIRINCHI, MAKSIMUM 20 qator.
/// `completionRate` = `completed / registered` — **`double?`** (PM tuzatmasi, 2026-09-02,
/// `docs/06` §8 qoidasi bilan bir xil: "ma'lumot yo'q bo'lsa `null`, hech qachon `0` emas").
/// `registered == 0` → `null` ("hali hech kim ro'yxatdan o'tmagan", nisbat ANIQLANMAGAN) —
/// HAQIQIY `0%` (ro'yxatdan o'tgan, lekin yakunlamagan) bilan chalkashtirilmasin.
/// `lastActivityAt` — shu OYNADAGI maktab sessiyalarining eng so'nggi `UpdatedAt`i (oynada
/// hech qanday sessiya bo'lmasa `null`).
/// </summary>
public sealed record AdminDashboardSchoolBreakdownItemDto(
    Guid SchoolId,
    string Name,
    string Region,
    int LinkViews,
    int Registered,
    int Completed,
    double? CompletionRate,
    DateTimeOffset? LastActivityAt);

/// <summary>`GET /api/admin/dashboard/stats?from=&to=` — `docs/07` 3.6-bo'lim to'liq javobi.</summary>
public sealed record AdminDashboardStatsDto(
    AdminDashboardTotalsDto Totals,
    AdminDashboardLast30DaysDto Last30Days,
    IReadOnlyList<AdminDashboardPersonalityItemDto> PersonalityDistribution,
    IReadOnlyList<AdminDashboardActivityItemDto> ActivityDistribution,
    IReadOnlyList<AdminDashboardHollandItemDto> HollandTop,
    IReadOnlyList<AdminDashboardRecentAssessmentDto> RecentAssessments,
    AdminDashboardFunnelDto Funnel,
    IReadOnlyList<AdminDashboardSchoolBreakdownItemDto> SchoolBreakdown);
