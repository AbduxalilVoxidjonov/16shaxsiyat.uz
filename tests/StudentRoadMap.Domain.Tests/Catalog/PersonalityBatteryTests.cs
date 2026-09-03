using FluentAssertions;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Domain.Tests.Catalog;

/// <summary>
/// `PersonalityBattery` — "shaxsiyat batareyasi bormi" savolining YAGONA domen qoidasi
/// (`docs/06` 8-bo'lim, 2026-09-02 qaror). Mezon metodika KODIGA bog'liq emas: eski frontend
/// mantiqi `"MBTI16"` satrini qidirardi va `Custom` dasturda jimgina noto'g'ri ishlardi.
/// </summary>
public sealed class PersonalityBatteryTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static TestDefinition CreateSystemBatteryTest(string code, string scoringStrategyCode = "SUM")
    {
        var id = Guid.NewGuid();
        var question = Question.Create(
            id: Guid.NewGuid(),
            testDefinitionId: id,
            code: $"{code}-Q01",
            displayOrder: 1,
            textUz: "Savol",
            questionType: QuestionType.Likert5,
            scale: "GEN",
            scaleDirection: 1,
            weight: 1.0m,
            isRequired: true,
            isSystem: true);

        return TestDefinition.CreateSystemPublished(
            id, code, $"{code} nomi", null, 1, 5, false, 10, scoringStrategyCode, [question], Now);
    }

    [Fact]
    public void Includes_SeedShaklidagiIlmiyMetodika_True()
    {
        // Kod ataylab `MBTI16` EMAS — mezon kodga bog'liq emasligini ko'rsatadi.
        var test = CreateSystemBatteryTest("SHAXS-V2");

        test.IsPersonalityBattery.Should().BeTrue();
        PersonalityBattery.Includes(test).Should().BeTrue();
    }

    [Fact]
    public void Includes_CustomBallanadiganAnketa_False()
    {
        // Kod ataylab `MBTI16` — satr solishtiruvchi mezon bu yerda xato `true` berardi.
        var test = TestDefinition.Create(
            Guid.NewGuid(), "MBTI16", "Maxsus anketa", 1, 10, "SUM", Now, kind: TestKind.Custom);

        test.IsPersonalityBattery.Should().BeFalse();
    }

    [Fact]
    public void Includes_SurveyRejimi_False()
    {
        // `Survey` ballanmaydi (`TestResult` yozilmaydi) — undan tip HECH QACHON chiqmaydi,
        // hatto `Standard` deb belgilangan bo'lsa ham.
        var test = TestDefinition.Create(
            Guid.NewGuid(), "SURVEY-1", "So'rovnoma", 1, 10, null, Now,
            kind: TestKind.Standard, scoringMode: TestScoringMode.Survey);

        test.IsPersonalityBattery.Should().BeFalse();
        PersonalityBattery.Includes(TestKind.Standard, TestScoringMode.Survey).Should().BeFalse();
    }

    [Fact]
    public void Includes_TizimMetodikasiningNusxasi_False()
    {
        // `Duplicate()` ataylab tahrirlanadigan `Custom` nusxa yaratadi — u endi ilmiy
        // batareya emas (`IsSystem` mezon sifatida yaramasligining ham sababi).
        var original = CreateSystemBatteryTest("SHAXS-V2");

        var copy = original.Duplicate(Guid.NewGuid(), "SHAXS-V2-COPY", Now);

        copy.IsPersonalityBattery.Should().BeFalse();
    }

    [Fact]
    public void ContainedIn_FaqatCustomVaSurveyDanIborat_False()
    {
        var custom = TestDefinition.Create(Guid.NewGuid(), "CUSTOM-1", "Maxsus", 1, 10, "SUM", Now);
        var survey = TestDefinition.Create(
            Guid.NewGuid(), "SURVEY-1", "So'rovnoma", 2, 10, null, Now, scoringMode: TestScoringMode.Survey);

        PersonalityBattery.ContainedIn([custom, survey]).Should().BeFalse();
    }

    [Fact]
    public void ContainedIn_KamidaBittaIlmiyMetodika_True()
    {
        var custom = TestDefinition.Create(Guid.NewGuid(), "CUSTOM-1", "Maxsus", 1, 10, "SUM", Now);
        var battery = CreateSystemBatteryTest("SHAXS-V2");

        PersonalityBattery.ContainedIn([custom, battery]).Should().BeTrue();
    }

    [Fact]
    public void ContainedIn_BoshRoyxat_False()
    {
        // Dasturda test bo'lmasa — "bor" emas, "yo'q" (ma'lumot yo'qligi `true` ga aylanmaydi).
        PersonalityBattery.ContainedIn([]).Should().BeFalse();
    }

    /// <summary>
    /// Rol xaritasi strategiya SINFLARINING o'z `StrategyCode` qiymatidan tekshiriladi — ya'ni
    /// `PersonalityBattery` ichidagi konstantalar `Domain/Scoring` registridan ajralib ketsa
    /// (masalan strategiya kodi o'zgarsa) shu test qizaradi va rol jimgina `None` ga aylanib
    /// qolmaydi.
    /// </summary>
    [Fact]
    public void RoleOf_HarBirBatareyaStrategiyasi_OzRoliniBeradi()
    {
        BatteryRoleFor(new Mbti16Strategy().StrategyCode).Should().Be(PersonalityBatteryRole.PersonalityType);
        BatteryRoleFor(new BigFiveStrategy().StrategyCode).Should().Be(PersonalityBatteryRole.Traits);
        BatteryRoleFor(new RiasecStrategy().StrategyCode).Should().Be(PersonalityBatteryRole.CareerInterest);
        BatteryRoleFor(new ActivityStrategy().StrategyCode).Should().Be(PersonalityBatteryRole.Activity);
    }

    [Fact]
    public void RoleOf_KodStrategiyaKodidanFarqQilsa_RolStrategiyadanKeladi()
    {
        // ⚠️ Xatoning O'ZAGI: anketa kodi `PERS-BAT-1`, lekin ballni `MBTI16` strategiyasi
        // hisoblaydi — natija baribir shaxsiyat tipi. Kod bo'yicha qidiruv buni ko'rmasdi.
        var test = CreateSystemBatteryTest("PERS-BAT-1", "MBTI16");

        PersonalityBattery.RoleOf(test).Should().Be(PersonalityBatteryRole.PersonalityType);
    }

    [Fact]
    public void RoleOf_MBTI16KodliCustomAnketa_None()
    {
        // Teskari holat: kod `MBTI16`, lekin anketa `Custom` va `SUM` bilan ballanadi — bu
        // shaxsiyat tipi EMAS. Satr solishtiruvchi mantiq bu yerda tipni "topib" olardi.
        var test = TestDefinition.Create(
            Guid.NewGuid(), "MBTI16", "Maxsus anketa", 1, 10, "SUM", Now, kind: TestKind.Custom);

        PersonalityBattery.RoleOf(test).Should().Be(PersonalityBatteryRole.None);
    }

    [Fact]
    public void RoleOf_BatareyaStrategiyasiCustomAnketada_None()
    {
        // `Includes` mezoni birinchi shart: `Custom` anketa batareyaga kirmaydi, demak undan
        // rol ham chiqmaydi — `hasPersonalityBattery = false` bo'lgan sessiyada tip/Holland
        // kodi paydo bo'lib qolmaydi (bitta qoida, ikkita joyda bir xil javob).
        PersonalityBattery.RoleOf(TestKind.Custom, TestScoringMode.Scored, "MBTI16")
            .Should().Be(PersonalityBatteryRole.None);
    }

    [Fact]
    public void RoleOf_SumStrategiyasiVaKodsizAnketa_None()
    {
        BatteryRoleFor(new SumStrategy().StrategyCode).Should().Be(PersonalityBatteryRole.None);
        BatteryRoleFor(null).Should().Be(PersonalityBatteryRole.None);
        BatteryRoleFor("   ").Should().Be(PersonalityBatteryRole.None);
    }

    /// <summary>Batareya mezoniga tushadigan anketa uchun rolni FAQAT strategiya kodi hal qiladi.</summary>
    private static PersonalityBatteryRole BatteryRoleFor(string? scoringStrategyCode) =>
        PersonalityBattery.RoleOf(TestKind.Standard, TestScoringMode.Scored, scoringStrategyCode);
}
