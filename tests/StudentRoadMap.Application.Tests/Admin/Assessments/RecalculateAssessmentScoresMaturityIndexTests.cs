using FluentAssertions;
using StudentRoadMap.Application.Admin.Assessments.RecalculateScores;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Tests.Admin.Assessments;

/// <summary>
/// `RecalculateAssessmentScoresCommandHandler.ApplyMaturityIndexIfPossible` — `MaturityIndex`
/// (`docs/03` §3.3) qaysi ikkita natijadan hisoblanishi.
///
/// <para>
/// ⚠️ 2026-09-03 topilmasi: bu handler ilgari natijani metodika KODI bo'yicha topardi
/// (`TestCode == "BIG5"` / `"ACTIVITY"`). `AssessmentProgram` kiritilgandan keyin bu JIMGINA
/// buziladi: `BIG5` KODLI `Custom` anketa haqiqiy batareya o'rniga ishlatiladi va hech qanday
/// xato ko'rsatilmaydi. Shu sabab bu testlarda anketa KODLARI ATAYLAB zid — haqiqiy batareya
/// `PERS-BAT-*` kodli (`Standard` + `Scored` + `BIG5`/`ACTIVITY` strategiyali), chalg'ituvchi
/// esa `BIG5`/`ACTIVITY` KODLI `Custom` (`SUM`) superadmin anketasi.
/// </para>
/// </summary>
public sealed class RecalculateAssessmentScoresMaturityIndexTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 10, 0, 0, TimeSpan.Zero);

    private static readonly Guid AssessmentId = Guid.NewGuid();

    /// <summary>Haqiqiy batareya: C/STABILITY/A/O = 80, SELF = 80 → `MaturityIndex` = 80.</summary>
    private const double ExpectedMaturityIndexFromBattery = 80;

    private static TestResult BigFiveShapedResult(Guid assessmentTestId, string testCode, double pct) => TestResult.Create(
        Guid.NewGuid(),
        assessmentTestId,
        AssessmentId,
        testCode,
        rawScoresJson: "{}",
        normalizedScoresJson: $$"""{"O":{{pct}},"C":{{pct}},"E":{{pct}},"A":{{pct}},"N":{{100 - pct}},"STABILITY":{{pct}}}""",
        scoringVersion: 1,
        testVersion: 1,
        computedAt: Now,
        levelsJson: """{"O":"Yuqori"}""");

    private static TestResult ActivityShapedResult(Guid assessmentTestId, string testCode, double pct) => TestResult.Create(
        Guid.NewGuid(),
        assessmentTestId,
        AssessmentId,
        testCode,
        rawScoresJson: "{}",
        normalizedScoresJson: $$"""{"MOT":{{pct}},"SELF":{{pct}},"SOCA":{{pct}},"ENG":{{pct}}}""",
        scoringVersion: 1,
        testVersion: 1,
        computedAt: Now,
        levelsJson: """{"ACTIVITY":"Moderate"}""");

    [Fact]
    public void ApplyMaturityIndexIfPossible_KodiZidBolganda_HaqiqiyBatareyadanHisoblanadi()
    {
        var batteryTraitsTestId = Guid.NewGuid();
        var batteryActivityTestId = Guid.NewGuid();
        var decoyTraitsTestId = Guid.NewGuid();
        var decoyActivityTestId = Guid.NewGuid();

        // Chalg'ituvchilar ATAYLAB ro'yxatning boshida — eski `FirstOrDefault(r => r.TestCode == ...)`
        // aynan ularni birinchi topardi.
        var decoyTraits = BigFiveShapedResult(decoyTraitsTestId, "BIG5", pct: 20);
        var decoyActivity = ActivityShapedResult(decoyActivityTestId, "ACTIVITY", pct: 20);
        var batteryTraits = BigFiveShapedResult(batteryTraitsTestId, "PERS-BAT-2", pct: 80);
        var batteryActivity = ActivityShapedResult(batteryActivityTestId, "PERS-BAT-4", pct: 80);

        var testResults = new List<TestResult> { decoyTraits, decoyActivity, batteryTraits, batteryActivity };

        // Rol xaritasi `PersonalityBatteryRoles.LoadByAssessmentTestIdAsync` bilan BIR XIL qoidada
        // quriladi: `None` rolli bloklar (chalg'ituvchilar) xaritaga UMUMAN kirmaydi.
        PersonalityBattery.RoleOf(TestKind.Custom, TestScoringMode.Scored, "SUM")
            .Should().Be(PersonalityBatteryRole.None, "`SUM` strategiyali `Custom` anketa batareyaga kirmaydi");

        var rolesByAssessmentTestId = new Dictionary<Guid, PersonalityBatteryRole>
        {
            [batteryTraitsTestId] = PersonalityBattery.RoleOf(TestKind.Standard, TestScoringMode.Scored, "BIG5"),
            [batteryActivityTestId] = PersonalityBattery.RoleOf(TestKind.Standard, TestScoringMode.Scored, "ACTIVITY"),
        };

        RecalculateAssessmentScoresCommandHandler.ApplyMaturityIndexIfPossible(testResults, rolesByAssessmentTestId);

        batteryTraits.CompositeIndex.Should().Be(
            ExpectedMaturityIndexFromBattery,
            "`MaturityIndex` `BIG5`/`ACTIVITY` STRATEGIYALI natijalardan hisoblanadi (eski satr solishtiruvida 20 chiqardi)");
        batteryTraits.LevelsJson.Should().Contain("MATURITY");

        decoyTraits.CompositeIndex.Should().BeNull("`BIG5` KODLI `Custom` anketaga `MaturityIndex` yozilmaydi");
        decoyTraits.LevelsJson.Should().NotContain("MATURITY");
    }

    [Fact]
    public void ApplyMaturityIndexIfPossible_BatareyasizDastur_HechNarsaYozilmaydi()
    {
        var decoyTraits = BigFiveShapedResult(Guid.NewGuid(), "BIG5", pct: 20);
        var decoyActivity = ActivityShapedResult(Guid.NewGuid(), "ACTIVITY", pct: 20);
        var testResults = new List<TestResult> { decoyTraits, decoyActivity };

        // Batareya yo'q — xarita BO'SH (`None` rolli bloklar unga kirmaydi).
        var rolesByAssessmentTestId = new Dictionary<Guid, PersonalityBatteryRole>();

        RecalculateAssessmentScoresCommandHandler.ApplyMaturityIndexIfPossible(testResults, rolesByAssessmentTestId);

        decoyTraits.CompositeIndex.Should().BeNull("batareyasiz dasturda `MaturityIndex` umuman hisoblanmaydi — `0` ham yozilmaydi");
        decoyActivity.CompositeIndex.Should().BeNull();
        decoyTraits.LevelsJson.Should().NotContain("MATURITY");
    }
}
