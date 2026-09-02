namespace StudentRoadMap.Application.Admin.Dashboard;

/// <summary>
/// `GetDashboardStatsQueryHandler` uchun sof arifmetika (DB'siz, sinf holatisiz) — atayin
/// ajratilgan (`Handle()`ning o'zi ham endi `AppDbContext.ApplySqliteDateTimeOffsetConversion`
/// bilan integratsiya sinov muhitida (SQLite) to'liq sinaladi, `prompts/15` 2-bosqich,
/// 2026-09-02 — bu ajratish ENDI zaruriyat emas, lekin foydali qoladi: HISOB-KITOB mantig'i
/// (nol bo'linish himoyasi, `prompts/15` MAXSUS DIQQAT #4) `Application.Tests`da DB'siz,
/// tezroq va HTTP/kesh holatidan mustaqil sinaladi).
/// </summary>
internal static class AdminDashboardMath
{
    /// <summary>`dropOffRate` = yakunlanmagan / jami boshlangan. `total == 0` → `0` (nol bo'linishsiz).</summary>
    public static double DropOffRate(int startedTotal, int notCompletedTotal) =>
        startedTotal == 0 ? 0.0 : (double)notCompletedTotal / startedTotal;

    /// <summary>O'rtacha (masalan ballar yig'indisi / soni). `count == 0` → `0`.</summary>
    public static double SafeAverage(decimal sum, int count) =>
        count == 0 ? 0.0 : (double)sum / count;
}
