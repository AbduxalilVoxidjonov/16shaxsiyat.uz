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
    /// <summary>
    /// `dropOffRate` = yakunlanmagan / jami boshlangan. `startedTotal == 0` → `null` — nol
    /// BO'LINISHSIZ EMAS, "ma'lumot yo'q" (`docs/06` §8, 2026-09-02 qaror: "ma'lumot yo'q bo'lsa
    /// `null`, hech qachon `0` emas" — `MaturityIndex`/`ActivityIndex`dagi bilan bir xil qoida,
    /// PM'ning 2026-09-02 topilmasi). Oynada HECH KIM boshlamagan (ma'lumot yo'q) bilan
    /// HAMMASI muvaffaqiyatli yakunlangan (0% tashlab ketish, HAQIQIY `0.0`) ajratiladi.
    /// </summary>
    public static double? DropOffRate(int startedTotal, int notCompletedTotal) =>
        startedTotal == 0 ? null : (double)notCompletedTotal / startedTotal;

    /// <summary>
    /// O'rtacha (masalan ballar yig'indisi / soni). `count == 0` → `null` (`0` EMAS — yuqoridagi
    /// `DropOffRate` izohidagi bilan bir xil sabab/qaror: "ma'lumot yo'q" bilan "o'rtacha
    /// haqiqatan 0" chalkashtirilmasin).
    /// </summary>
    public static double? SafeAverage(decimal sum, int count) =>
        count == 0 ? null : (double)sum / count;

    /// <summary>
    /// `schoolBreakdown.completionRate` = `completed / registered`. `registered == 0` → `null`
    /// (PM tuzatmasi, 2026-09-02 — `DropOffRate`/`SafeAverage` bilan BIR XIL qoida: "ma'lumot
    /// yo'q" (hali hech kim ro'yxatdan o'tmagan) bilan "yakunlash foizi HAQIQIY 0%" (ro'yxatdan
    /// o'tgan, lekin hech kim yakunlamagan) chalkashtirilmasin — frontend `null`ni `—` deb
    /// ko'rsatadi).
    /// </summary>
    public static double? CompletionRate(int completed, int registered) =>
        registered == 0 ? null : (double)completed / registered;
}
