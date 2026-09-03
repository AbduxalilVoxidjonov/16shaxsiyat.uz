using System.Globalization;
using FluentAssertions;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Application.Tests.Admin.Students.Testing;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Tests.Admin.Students;

/// <summary>
/// `StudentProfileMapping.BuildTestResultsAsync` — `docs/07` 3.2 `results` bloki QAYSI natijadan
/// to'ldiriladi.
///
/// <para>
/// ⚠️ 2026-09-03 topilmasi (ettinchi, oxirgi qoldiq): bu moslashtirish natijani metodika KODI
/// bo'yicha topardi — `TestCode == "MBTI16" / "BIG5" / "RIASEC" / "ACTIVITY"`. `AssessmentProgram`
/// tushunchasidan keyin bu JIMGINA buziladi: `MBTI16` KODLI `Custom` anketa haqiqiy batareya
/// o'rniga admin javobiga tushadi va hech qanday xato ko'rsatilmaydi. Xato ikkala yo'lda ham
/// ko'rinardi — `GET /api/admin/students/{id}` va `POST .../recalculate-scores`.
/// </para>
///
/// <para>
/// Shu sabab bu testlarda anketa KODLARI ATAYLAB zid: haqiqiy batareya `PERS-BAT-1..4` kodli
/// (`Standard` + `Scored` + tegishli `ScoringStrategyCode`), chalg'ituvchi esa
/// `MBTI16`/`BIG5`/`RIASEC`/`ACTIVITY` KODLI `Custom` (`SUM`) superadmin anketasi.
/// **JSON kalitlari o'zgarmaydi** — o'zgargani faqat qaysi natija qaysi kalitga tushishi.
/// </para>
/// </summary>
public sealed class StudentProfileTestResultsMappingTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 10, 0, 0, TimeSpan.Zero);

    private static readonly Guid AssessmentId = Guid.NewGuid();

    private sealed record Fixture(
        FakeStudentProfileAppDbContext Context,
        StudentProfileInlineAsyncQueryExecutor Executor,
        List<TestResult> TestResults);

    /// <summary>
    /// Sessiya: 4 ta HAQIQIY batareya bloki (`PERS-BAT-1..4`) va 4 ta chalg'ituvchi `Custom`
    /// anketa (`MBTI16`/`BIG5`/`RIASEC`/`ACTIVITY` KODLARI bilan). Chalg'ituvchilar ATAYLAB
    /// ro'yxatning BOSHIDA — eski `FirstOrDefault(r => r.TestCode == ...)` aynan ularni topardi.
    /// </summary>
    private static Fixture BuildFixture(bool includeBattery)
    {
        var context = new FakeStudentProfileAppDbContext();
        var executor = new StudentProfileInlineAsyncQueryExecutor();

        context.TypeCatalogList.Add(TypeCatalogEntry.Create("INTJ", "Loyihachi", "Qisqa tavsif", "Uzun tavsif"));
        context.TypeCatalogList.Add(TypeCatalogEntry.Create("ESFP", "Chalg'ituvchi tip", "Qisqa tavsif", "Uzun tavsif"));
        context.CareerMapList.Add(CareerMapEntry.Create(Guid.NewGuid(), "IR", "Muhandislik va texnika", 1, null, ["Muhandis"]));

        var testResults = new List<TestResult>();

        // Chalg'ituvchilar: batareya KODLARI, lekin `Custom` + `SUM` (rol — `None`).
        AddBlock(context, testResults, "MBTI16", TestKind.Custom, "SUM", MbtiShaped("ESFP", pct: 90, letter: "E"));
        AddBlock(context, testResults, "BIG5", TestKind.Custom, "SUM", BigFiveShaped(pct: 20));
        AddBlock(context, testResults, "RIASEC", TestKind.Custom, "SUM", RiasecShaped("ZZZ", pct: 20));
        AddBlock(context, testResults, "ACTIVITY", TestKind.Custom, "SUM", ActivityShaped(pct: 20, level: "Past"));

        if (includeBattery)
        {
            AddBlock(context, testResults, "PERS-BAT-1", TestKind.Standard, "MBTI16", MbtiShaped("INTJ", pct: 28.3, letter: "I"));
            AddBlock(context, testResults, "PERS-BAT-2", TestKind.Standard, "BIG5", BigFiveShaped(pct: 80));
            AddBlock(context, testResults, "PERS-BAT-3", TestKind.Standard, "RIASEC", RiasecShaped("IRA", pct: 80));
            AddBlock(context, testResults, "PERS-BAT-4", TestKind.Standard, "ACTIVITY", ActivityShaped(pct: 80, level: "Yuqori"));
        }

        return new Fixture(context, executor, testResults);
    }

    private static void AddBlock(
        FakeStudentProfileAppDbContext context,
        List<TestResult> testResults,
        string code,
        TestKind kind,
        string scoringStrategyCode,
        Func<Guid, string, TestResult> resultFactory)
    {
        var definitionId = Guid.NewGuid();
        var assessmentTestId = Guid.NewGuid();

        context.TestDefinitionList.Add(TestDefinition.Create(
            definitionId,
            code,
            $"{code} nomi",
            displayOrder: context.TestDefinitionList.Count + 1,
            estimatedMinutes: 5,
            scoringStrategyCode: scoringStrategyCode,
            now: Now,
            kind: kind,
            scoringMode: TestScoringMode.Scored));

        context.AssessmentTestList.Add(AssessmentTest.Create(
            assessmentTestId, AssessmentId, definitionId, context.AssessmentTestList.Count + 1, totalCount: 1));

        testResults.Add(resultFactory(assessmentTestId, code));
    }

    /// <summary>JSON raqami HAR DOIM invariant (nuqtali) — mahalliy madaniyatda `28,3` yozilib qolmasligi uchun.</summary>
    private static string Num(double value) => value.ToString(CultureInfo.InvariantCulture);

    private static Func<Guid, string, TestResult> MbtiShaped(string resultCode, double pct, string letter) =>
        (assessmentTestId, testCode) => TestResult.Create(
            Guid.NewGuid(),
            assessmentTestId,
            AssessmentId,
            testCode,
            rawScoresJson: "{}",
            normalizedScoresJson: $$"""{"EI":{{Num(pct)}},"SN":{{Num(pct)}},"TF":{{Num(pct)}},"JP":{{Num(pct)}}}""",
            scoringVersion: 1,
            testVersion: 1,
            computedAt: Now,
            resultCode: resultCode,
            levelsJson: $$"""{"EI":"{{letter}}","SN":"N","TF":"T","JP":"J"}""");

    private static Func<Guid, string, TestResult> BigFiveShaped(double pct) =>
        (assessmentTestId, testCode) => TestResult.Create(
            Guid.NewGuid(),
            assessmentTestId,
            AssessmentId,
            testCode,
            rawScoresJson: $$"""{"O":{{Num(pct)}},"C":{{Num(pct)}},"E":{{Num(pct)}},"A":{{Num(pct)}},"N":{{Num(100 - pct)}}}""",
            normalizedScoresJson: $$"""{"O":{{Num(pct)}},"C":{{Num(pct)}},"E":{{Num(pct)}},"A":{{Num(pct)}},"N":{{Num(100 - pct)}},"STABILITY":{{Num(pct)}}}""",
            scoringVersion: 1,
            testVersion: 1,
            computedAt: Now,
            levelsJson: """{"O":"Yuqori","C":"Yuqori","E":"Yuqori","A":"Yuqori","N":"Past"}""",
            compositeIndex: pct);

    private static Func<Guid, string, TestResult> RiasecShaped(string resultCode, double pct) =>
        (assessmentTestId, testCode) => TestResult.Create(
            Guid.NewGuid(),
            assessmentTestId,
            AssessmentId,
            testCode,
            rawScoresJson: "{}",
            normalizedScoresJson: $$"""{"R":{{Num(pct)}},"I":{{Num(pct)}},"ART":{{Num(pct)}},"SOC":10,"ENT":10,"CONV":10,"DIFFERENTIATION":{{Num(pct)}}}""",
            scoringVersion: 1,
            testVersion: 1,
            computedAt: Now,
            resultCode: resultCode,
            levelsJson: """{"CONSISTENCY":"Yuqori"}""");

    private static Func<Guid, string, TestResult> ActivityShaped(double pct, string level) =>
        (assessmentTestId, testCode) => TestResult.Create(
            Guid.NewGuid(),
            assessmentTestId,
            AssessmentId,
            testCode,
            rawScoresJson: "{}",
            normalizedScoresJson: $$"""{"MOT":{{Num(pct)}},"SELF":{{Num(pct)}},"SOCA":{{Num(pct)}},"ENG":{{Num(pct)}}}""",
            scoringVersion: 1,
            testVersion: 1,
            computedAt: Now,
            levelsJson: $$"""{"ACTIVITY":"{{level}}"}""",
            compositeIndex: pct);

    /// <summary>
    /// <b>Eski kodda</b> (`TestCode == "MBTI16"` va h.k.): `results.MBTI16.resultCode` = `ESFP`
    /// (`typeName` = "Chalg'ituvchi tip", `axes.EI.pct` = 90, harf `E`), `results.BIG5.factors.O.pct`
    /// = 20, `results.RIASEC.resultCode` = `ZZZ` (kasb yo'nalishlari BO'SH), `results.ACTIVITY.
    /// activityIndex` = 20 — ya'ni superadmin anketasining natijasi haqiqiy batareya o'rnida.
    /// </summary>
    [Fact]
    public async Task BuildTestResults_KodiZidBolganda_NatijaHaqiqiyBatareyadanKeladi()
    {
        var fixture = BuildFixture(includeBattery: true);

        var results = await StudentProfileMapping.BuildTestResultsAsync(
            fixture.TestResults, fixture.Context, fixture.Executor, CancellationToken.None);

        results.Mbti16.Should().NotBeNull();
        results.Mbti16!.ResultCode.Should().Be("INTJ", "tip `MBTI16` STRATEGIYALI batareyadan olinadi (eski kodda `ESFP` chiqardi)");
        results.Mbti16.TypeName.Should().Be("Loyihachi");
        results.Mbti16.Axes["EI"].Pct.Should().Be(28.3);
        results.Mbti16.Axes["EI"].Letter.Should().Be("I");

        results.Big5.Should().NotBeNull();
        results.Big5!.Factors["O"].Pct.Should().Be(80, "eski kodda `BIG5` KODLI `Custom` anketaning 20 foizi chiqardi");
        results.Big5.StabilityPct.Should().Be(80);
        results.Big5.MaturityIndex.Should().Be(80);

        results.Riasec.Should().NotBeNull();
        results.Riasec!.ResultCode.Should().Be("IRA", "Holland kodi `RIASEC` STRATEGIYALI batareyadan olinadi (eski kodda `ZZZ`)");
        results.Riasec.CareerFields.Should().ContainSingle(f => f.Name == "Muhandislik va texnika")
            .Which.Professions.Should().Contain("Muhandis");

        results.Activity.Should().NotBeNull();
        results.Activity!.ActivityIndex.Should().Be(80, "eski kodda 20 chiqardi");
        results.Activity.ActivityLevel.Should().Be("Yuqori");
    }

    /// <summary>
    /// Batareyasiz dastur (faqat `Custom` anketalar): tegishli bloklar `null` — bo'sh yoki NOL
    /// qiymatli obyekt EMAS (`docs/06` qarorlar jurnali, 2026-09-02: "ma'lumot yo'q" ≠ "nol").
    /// Eski kod bu yerda `Custom` anketalarni batareya deb ko'rsatardi.
    /// </summary>
    [Fact]
    public async Task BuildTestResults_BatareyasizDastur_BloklarNullQaytadi()
    {
        var fixture = BuildFixture(includeBattery: false);

        var results = await StudentProfileMapping.BuildTestResultsAsync(
            fixture.TestResults, fixture.Context, fixture.Executor, CancellationToken.None);

        results.Mbti16.Should().BeNull("`MBTI16` KODLI `Custom` anketa batareya EMAS");
        results.Big5.Should().BeNull();
        results.Riasec.Should().BeNull();
        results.Activity.Should().BeNull();
    }

    /// <summary>Natija umuman yo'q — bloklar `null`, bazaga bitta ham so'rov ketmaydi.</summary>
    [Fact]
    public async Task BuildTestResults_NatijaYoq_SoRovsizNullQaytadi()
    {
        var fixture = BuildFixture(includeBattery: true);

        var results = await StudentProfileMapping.BuildTestResultsAsync(
            [], fixture.Context, fixture.Executor, CancellationToken.None);

        results.Mbti16.Should().BeNull();
        results.Big5.Should().BeNull();
        results.Riasec.Should().BeNull();
        results.Activity.Should().BeNull();
        fixture.Executor.QueryCount.Should().Be(0);
    }

    /// <summary>
    /// Rol xaritasi BITTA so'rov bilan yuklanadi — N+1 yo'q. Qolgan so'rovlar: `TypeCatalog`
    /// (`typeName`) va `CareerMap` (kasb yo'nalishlari), ya'ni jami 3 ta.
    /// </summary>
    [Fact]
    public async Task BuildTestResults_RolXaritasi_BittaSoRovBilanYuklanadi()
    {
        var fixture = BuildFixture(includeBattery: true);

        await StudentProfileMapping.BuildTestResultsAsync(
            fixture.TestResults, fixture.Context, fixture.Executor, CancellationToken.None);

        fixture.Executor.QueryCount.Should().Be(3, "rol xaritasi (1) + `TypeCatalog` (1) + `CareerMap` (1)");
    }
}
