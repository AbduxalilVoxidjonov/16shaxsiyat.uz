using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Jobs;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Tests.Testing;

namespace StudentRoadMap.Infrastructure.Tests.Jobs;

/// <summary>
/// `FallbackReportBuilder` — barcha AI provayderlari muvaffaqiyatsiz bo'lganda quriladigan
/// shablon hisobot (`docs/09` 11-bo'lim, P18).
///
/// <para>
/// ⚠️ 2026-09-03 topilmasi: shablon hisobot qaysi natijadan (tip / kasb qiziqishi) qurilishini
/// metodika KODI (`TestCode == "MBTI16"`) emas, `PersonalityBattery.RoleOf` (ya'ni
/// `TestDefinition.ScoringStrategyCode`) belgilaydi. Shu sabab bu testlarda anketa KODLARI
/// ATAYLAB zid: haqiqiy batareya `PERS-BAT-*` kodli, chalg'ituvchi esa `MBTI16`/`RIASEC`
/// KODLI `Custom` (`SUM`) superadmin anketasi.
/// </para>
/// </summary>
public sealed class FallbackReportBuilderTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 10, 0, 0, TimeSpan.Zero);

    private const string BatteryTypeCode = "PERS-BAT-1";

    private const string BatteryCareerCode = "PERS-BAT-3";

    private static AppDbContext NewContext(SqliteConnection connection) =>
        SqliteAppDbContextFactory.CreateContext(connection, new FixedDateTimeProvider(Now), new NoOpPublisher());

    private sealed record TestBlockSpec(string Code, string ScoringStrategyCode, TestKind Kind, TestScoringMode ScoringMode);

    private sealed record SeededSession(Assessment Assessment, IReadOnlyDictionary<string, Guid> AssessmentTestIdByCode)
    {
        public Guid AssessmentTestId(string code) => AssessmentTestIdByCode[code];
    }

    private static async Task<SeededSession> SeedSessionAsync(SqliteConnection connection, params TestBlockSpec[] blocks)
    {
        await using var setup = NewContext(connection);
        await setup.Database.EnsureCreatedAsync();

        var school = School.Create(
            Guid.NewGuid(), "Test maktabi", "Toshkent", "Chilonzor",
            SchoolSlug.Create($"maktab-{Guid.NewGuid():N}").Value, $"access-{Guid.NewGuid():N}", Now);

        var student = Student.Create(
            Guid.NewGuid(), school.Id, "Alisher Karimov", new DateOnly(2010, 5, 20), Gender.Male, 9,
            PhoneNumber.Create("901234567").Value, Now, Now);

        var program = AssessmentProgram.Create(Guid.NewGuid(), $"PROG-{Guid.NewGuid():N}"[..12], "Sinov dasturi", Now);
        var assessment = Assessment.Create(
            Guid.NewGuid(), student.Id, school.Id, $"tok-{Guid.NewGuid():N}", "uz", program.Id, Now, Now.AddDays(7), Now);

        var assessmentTestIdByCode = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var displayOrder = 1;

        foreach (var block in blocks)
        {
            var definition = TestDefinition.Create(
                Guid.NewGuid(), block.Code, $"{block.Code} anketasi", displayOrder, estimatedMinutes: 5,
                scoringStrategyCode: block.ScoringStrategyCode, now: Now,
                kind: block.Kind, scoringMode: block.ScoringMode);
            setup.TestDefinitions.Add(definition);

            var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, definition.Id, displayOrder, totalCount: 1);
            assessment.AddTest(assessmentTest);
            assessmentTestIdByCode[block.Code] = assessmentTest.Id;
            displayOrder++;
        }

        setup.Add(school);
        setup.Add(student);
        setup.Add(program);
        setup.Add(assessment);
        await setup.SaveChangesAsync();

        return new SeededSession(assessment, assessmentTestIdByCode);
    }

    private static TestResult Result(SeededSession session, string testCode, string resultCode) => TestResult.Create(
        Guid.NewGuid(),
        session.AssessmentTestId(testCode),
        session.Assessment.Id,
        testCode,
        rawScoresJson: "{}",
        normalizedScoresJson: "{}",
        scoringVersion: 1,
        testVersion: 1,
        computedAt: Now,
        resultCode: resultCode,
        levelsJson: "{}");

    /// <summary>
    /// Eski kodda (`testResults.FirstOrDefault(r => r.TestCode == "MBTI16")`) shablon hisobot
    /// `MBTI16` KODLI `Custom` anketaning `ESFP` natijasidan qurilardi — `summary` da
    /// "Chalg'ituvchi tip" nomi chiqardi, haqiqiy `PERS-BAT-1` (`MBTI16` strategiyali) natija
    /// esa umuman ishlatilmasdi. Xuddi shu holat `RIASEC` uchun — kasb yo'nalishlari noto'g'ri
    /// Holland kodidan tanlanardi.
    /// </summary>
    [Fact]
    public async Task BuildAsync_KodiZidBolganda_ShablonHisobotHaqiqiyBatareyadanQuriladi()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();

        var session = await SeedSessionAsync(
            connection,
            new TestBlockSpec(BatteryTypeCode, "MBTI16", TestKind.Standard, TestScoringMode.Scored),
            new TestBlockSpec(BatteryCareerCode, "RIASEC", TestKind.Standard, TestScoringMode.Scored),
            new TestBlockSpec("MBTI16", "SUM", TestKind.Custom, TestScoringMode.Scored),
            new TestBlockSpec("RIASEC", "SUM", TestKind.Custom, TestScoringMode.Scored));

        await using (var catalog = NewContext(connection))
        {
            catalog.TypeCatalog.Add(TypeCatalogEntry.Create(
                "INTJ", "Loyihachi", "Haqiqiy batareya tavsifi", "Uzun tavsif"));
            catalog.TypeCatalog.Add(TypeCatalogEntry.Create(
                "ESFP", "Chalg'ituvchi tip", "Bu tavsif hisobotga tushmasligi kerak", "Uzun tavsif"));
            catalog.CareerMap.Add(CareerMapEntry.Create(Guid.NewGuid(), "IR", "Muhandislik", 1));
            catalog.CareerMap.Add(CareerMapEntry.Create(Guid.NewGuid(), "SE", "Chalg'ituvchi yo'nalish", 2));
            await catalog.SaveChangesAsync();
        }

        // Chalg'ituvchilar ATAYLAB ro'yxatning boshida — `FirstOrDefault` ularni birinchi topadi.
        var testResults = new List<TestResult>
        {
            Result(session, "MBTI16", "ESFP"),
            Result(session, "RIASEC", "SEC"),
            Result(session, BatteryTypeCode, "INTJ"),
            Result(session, BatteryCareerCode, "IRA"),
        };

        await using var context = NewContext(connection);

        var content = await FallbackReportBuilder.BuildAsync(
            session.Assessment.Id, testResults, context, new EfAsyncQueryExecutor(), CancellationToken.None);

        content.Summary.Should().Contain(
            "Loyihachi",
            "tip `MBTI16` STRATEGIYALI `PERS-BAT-1` natijasidan olinadi, kodi `MBTI16` bo'lgan `Custom` anketadan emas");
        content.Summary.Should().NotContain("Chalg'ituvchi tip");
        content.PersonalityPortrait.Should().NotContain("Bu tavsif hisobotga tushmasligi kerak");

        var careerFields = JsonDocument.Parse(content.CareerSuggestionsJson).RootElement
            .EnumerateArray()
            .Select(e => e.GetProperty("field").GetString())
            .ToList();

        careerFields.Should().Contain("Muhandislik", "kasb yo'nalishlari `RIASEC` STRATEGIYALI natijaning Holland kodidan (`IRA`) tanlanadi");
        careerFields.Should().NotContain("Chalg'ituvchi yo'nalish");
    }

    /// <summary>
    /// Batareyasiz dastur: hisobot HAM tip nomini, HAM kasb yo'nalishlarini o'ylab topmaydi —
    /// umumiy ("ma'lumot yo'q") matn qaytadi. Eski kodda `MBTI16` KODLI `Custom` anketa
    /// hisobotga "sizning tipingiz" jumlasini qo'shib qo'yardi.
    /// </summary>
    [Fact]
    public async Task BuildAsync_BatareyasizDastur_UmumiyShablonQaytaradi()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();

        var session = await SeedSessionAsync(
            connection,
            new TestBlockSpec("MBTI16", "SUM", TestKind.Custom, TestScoringMode.Scored));

        await using (var catalog = NewContext(connection))
        {
            catalog.TypeCatalog.Add(TypeCatalogEntry.Create(
                "ESFP", "Chalg'ituvchi tip", "Bu tavsif hisobotga tushmasligi kerak", "Uzun tavsif"));
            await catalog.SaveChangesAsync();
        }

        var testResults = new List<TestResult> { Result(session, "MBTI16", "ESFP") };

        await using var context = NewContext(connection);

        var content = await FallbackReportBuilder.BuildAsync(
            session.Assessment.Id, testResults, context, new EfAsyncQueryExecutor(), CancellationToken.None);

        content.Summary.Should().NotContain("Chalg'ituvchi tip");
        content.Summary.Should().Contain("To'liq shaxsiylashtirilgan tahlil tayyor bo'lgach");
        content.PersonalityPortrait.Should().Contain("Shaxsiyat portreti hozircha AI orqali tuzilmadi");
        content.CareerSuggestionsJson.Should().Be("[]");
    }
}
