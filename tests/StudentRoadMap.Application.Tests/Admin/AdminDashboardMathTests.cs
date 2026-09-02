using FluentAssertions;
using StudentRoadMap.Application.Admin.Dashboard;

namespace StudentRoadMap.Application.Tests.Admin;

/// <summary>
/// `GetDashboardStatsQueryHandler`ning DB'siz arifmetikasi — `prompts/15` MAXSUS DIQQAT #4
/// ("nol bo'linishga ehtiyot bo'l"). `GetDashboardStatsQueryHandler`ning o'zi (EF Core
/// so'rovlari orqali) integratsiya sinov muhitida (SQLite) `DateTimeOffset` predikatlari
/// (`WHERE`, `ORDER BY`) tarjima qilinmagani sabab to'liq HTTP darajasida sinalmaydi
/// (`AdminDashboardStatsEndpointTests`dagi `Skip` izohiga qarang — bu MUAMMO faqat SQLite'ga
/// xos, Postgres'da (production) ishlaydi) — shu sabab hisob-kitob mantig'i ATAYLAB shu
/// sof, tez, DB'siz testlar bilan qamrab olinadi.
/// </summary>
public sealed class AdminDashboardMathTests
{
    [Theory]
    [InlineData(10, 0, 0.0)]
    [InlineData(10, 10, 1.0)]
    [InlineData(4, 1, 0.25)]
    public void DropOffRate_TurliQiymatlar_ToGriHisoblanadi(int startedTotal, int notCompletedTotal, double expected)
    {
        AdminDashboardMath.DropOffRate(startedTotal, notCompletedTotal).Should().Be(expected);
    }

    /// <summary>
    /// PM topilmasi (2026-09-02, `docs/06` §8): `startedTotal == 0` → `null` (HAQIQIY `0` EMAS
    /// — "hali hech kim boshlamagan" bilan "0% tashlab ketish" chalkashtirilmasin).
    /// </summary>
    [Fact]
    public void DropOffRate_JamiBoshlanganNol_NullQaytaradi()
    {
        var act = () => AdminDashboardMath.DropOffRate(0, 0);

        act.Should().NotThrow();
        act().Should().BeNull();
    }

    /// <summary>PM topilmasi (2026-09-02, `docs/06` §8): `count == 0` → `null` (HAQIQIY `0` EMAS — "ma'lumot yo'q").</summary>
    [Fact]
    public void SafeAverage_SoniNol_NullQaytaradi()
    {
        AdminDashboardMath.SafeAverage(sum: 500m, count: 0).Should().BeNull();
    }

    [Fact]
    public void SafeAverage_ToGriOrtachaniHisoblaydi()
    {
        AdminDashboardMath.SafeAverage(sum: 300m, count: 4).Should().Be(75.0);
    }

    /// <summary>`schoolBreakdown.completionRate` — `prompts/15` vazifa 2 / PM tuzatmasi (2026-09-02).</summary>
    [Theory]
    [InlineData(0, 5, 0.0)]
    [InlineData(2, 5, 0.4)]
    [InlineData(5, 5, 1.0)]
    public void CompletionRate_TurliQiymatlar_ToGriHisoblanadi(int completed, int registered, double expected)
    {
        AdminDashboardMath.CompletionRate(completed, registered).Should().Be(expected);
    }

    /// <summary>
    /// PM tuzatmasi (2026-09-02, `docs/06` §8): `registered == 0` → `null` (HAQIQIY `0%` EMAS —
    /// "hali hech kim ro'yxatdan o'tmagan" bilan chalkashtirilmasin).
    /// </summary>
    [Fact]
    public void CompletionRate_RegisteredNol_NullQaytaradi()
    {
        var act = () => AdminDashboardMath.CompletionRate(completed: 3, registered: 0);

        act.Should().NotThrow();
        act().Should().BeNull();
    }
}
