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
    [InlineData(0, 0, 0.0)]
    [InlineData(10, 0, 0.0)]
    [InlineData(10, 10, 1.0)]
    [InlineData(4, 1, 0.25)]
    public void DropOffRate_TurliQiymatlar_ToGriHisoblanadi(int startedTotal, int notCompletedTotal, double expected)
    {
        AdminDashboardMath.DropOffRate(startedTotal, notCompletedTotal).Should().Be(expected);
    }

    [Fact]
    public void DropOffRate_JamiBoshlanganNol_NolBolinishTashlamaydi()
    {
        var act = () => AdminDashboardMath.DropOffRate(0, 0);

        act.Should().NotThrow();
        act().Should().Be(0.0);
    }

    [Fact]
    public void SafeAverage_SoniNol_NolQaytaradi()
    {
        AdminDashboardMath.SafeAverage(sum: 500m, count: 0).Should().Be(0.0);
    }

    [Fact]
    public void SafeAverage_ToGriOrtachaniHisoblaydi()
    {
        AdminDashboardMath.SafeAverage(sum: 300m, count: 4).Should().Be(75.0);
    }
}
