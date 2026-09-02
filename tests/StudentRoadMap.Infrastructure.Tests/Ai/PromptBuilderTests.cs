using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Ai;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Tests.Testing;

namespace StudentRoadMap.Infrastructure.Tests.Ai;

/// <summary>
/// `PromptBuilder` — `docs/09-ai-analiz-moduli.md` 3-bo'lim (`AnalysisInput` qurilishi) va
/// `prompts/16` MAJBURIY talabi: chiqishda shaxsiy ma'lumot yo'qligini tekshiruvchi test.
/// Haqiqiy `AppDbContext` (SQLite in-memory, `SqliteAppDbContextFactory` — `DbSeederTests`
/// bilan bir xil naqsh) ustida ishlaydi — `TypeCatalog`/`CareerMap`/`TestDefinition`
/// qidiruvlari haqiqiy so'rov sifatida tekshiriladi.
/// </summary>
public sealed class PromptBuilderTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 10, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNameCaseInsensitive = true };

    private static AppDbContext NewContext(SqliteConnection connection) =>
        SqliteAppDbContextFactory.CreateContext(connection, new FixedDateTimeProvider(Now), new NoOpPublisher());

    private static PhoneNumber Phone(string raw) => PhoneNumber.Create(raw).Value;

    private static Student CreateStudent(
        string fullName = "Alisher Karimov",
        DateOnly? birthDate = null,
        Gender gender = Gender.Male,
        int grade = 9,
        string phone = "901234567",
        string? email = "alisher.karimov@example.com") =>
        Student.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            fullName,
            birthDate ?? new DateOnly(2010, 5, 20),
            gender,
            grade,
            Phone(phone),
            Now,
            Now,
            email: email);

    private static Assessment CreateAssessment(double reliabilityScore = 82.5, ReliabilityFlag flag = ReliabilityFlag.Reliable, string language = "uz")
    {
        var assessment = Assessment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "session-token-xyz", language, Guid.NewGuid(), Now, Now.AddDays(7), Now);
        assessment.SetReliability(reliabilityScore, flag, Now);
        return assessment;
    }

    private static TestResult Mbti16Result(Guid assessmentId, string resultCode = "INTJ") => TestResult.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        assessmentId,
        "MBTI16",
        rawScoresJson: "{}",
        normalizedScoresJson: """{"EI":28.3,"SN":71.6,"TF":33.3,"JP":64.1}""",
        scoringVersion: 1,
        testVersion: 1,
        computedAt: Now,
        resultCode: resultCode,
        levelsJson: """{"EI":"I","SN":"N","TF":"T","JP":"J"}""",
        flagsJson: """["Borderline:TF"]""");

    private static TestResult Big5Result(Guid assessmentId) => TestResult.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        assessmentId,
        "BIG5",
        rawScoresJson: "{}",
        normalizedScoresJson: """{"O":70,"C":77.5,"E":35,"A":62.5,"N":30,"STABILITY":70}""",
        scoringVersion: 1,
        testVersion: 1,
        computedAt: Now,
        levelsJson: """{"O":"Yuqori","C":"Yuqori","E":"Past","A":"Yuqori","N":"Past","MATURITY":"Yaxshi"}""",
        compositeIndex: 68.4);

    private static TestResult RiasecResult(Guid assessmentId, string resultCode = "IRA") => TestResult.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        assessmentId,
        "RIASEC",
        rawScoresJson: "{}",
        normalizedScoresJson: """{"R":62,"I":88,"ART":71,"SOC":40,"ENT":35,"CONV":48,"DIFFERENTIATION":53}""",
        scoringVersion: 1,
        testVersion: 1,
        computedAt: Now,
        resultCode: resultCode,
        levelsJson: """{"CONSISTENCY":"High"}""");

    private static TestResult ActivityResult(Guid assessmentId, bool needsAttention = false) => TestResult.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        assessmentId,
        "ACTIVITY",
        rawScoresJson: "{}",
        normalizedScoresJson: """{"MOT":74,"SELF":68,"SOCA":52,"ENG":60}""",
        scoringVersion: 1,
        testVersion: 1,
        computedAt: Now,
        levelsJson: """{"ACTIVITY":"Moderate"}""",
        compositeIndex: 65.2,
        flagsJson: needsAttention ? """["NeedsAttention"]""" : "[]");

    private static TestResult CustomResult(Guid assessmentId, string testCode = "STRESS01") => TestResult.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        assessmentId,
        testCode,
        rawScoresJson: "{}",
        normalizedScoresJson: """{"STRESS":58,"SUPPORT":74}""",
        scoringVersion: 1,
        testVersion: 1,
        computedAt: Now,
        levelsJson: """{"STRESS":"O'rtacha","SUPPORT":"Yuqori"}""");

    // --- MAJBURIY: shaxsiy ma'lumot chiqishda yo'qligi ------------------------------------

    [Fact]
    public async Task BuildAsync_OutputNeverContainsStudentPersonalIdentifiers()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using (var setup = NewContext(connection))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        // Haqiqiy PII bilan to'ldirilgan Student — F.I.Sh., telefon, email, aniq tug'ilgan sana.
        var student = CreateStudent(
            fullName: "G'ulomjonov Sardorbek Baxtiyor o'g'li",
            birthDate: new DateOnly(2010, 5, 20),
            phone: "907654321",
            email: "sardorbek.gulomjonov@salohiyat-maktab.uz");
        var assessment = CreateAssessment();
        var testResults = new List<TestResult>
        {
            Mbti16Result(assessment.Id),
            Big5Result(assessment.Id),
            RiasecResult(assessment.Id),
            ActivityResult(assessment.Id),
        };

        await using var context = NewContext(connection);
        var builder = new PromptBuilder(context, new EfAsyncQueryExecutor());

        var result = await builder.BuildAsync(student, assessment, testResults, Now);

        // `PromptBuilder.BuildAsync` `Student`/`Assessment`dan tashqari HECH NARSANI (masalan
        // `School`) qabul qilmaydi — shu sabab "maktab nomi" tuzilma darajasida sizib chiqishi
        // MUMKIN EMAS. Qolgan to'rttasi (ism, telefon, email, aniq tug'ilgan sana) esa `Student`
        // orqali PromptBuilderga kirib keladi va FAOL ravishda chiqarib tashlanishi kerak — shu
        // sabab ular runtime assertsiya bilan tekshiriladi.
        var output = result.AnalysisInputJson + " " + result.UserText;

        output.Should().NotContain("G'ulomjonov", "F.I.Sh. familiyasi chiqmasligi kerak");
        output.Should().NotContain("Sardorbek", "F.I.Sh. ismi chiqmasligi kerak");
        output.Should().NotContain("Baxtiyor", "F.I.Sh.ning otasining ismi chiqmasligi kerak");
        output.Should().NotContain("907654321", "telefon raqami chiqmasligi kerak");
        output.Should().NotContain("+998907654321", "normallashtirilgan telefon raqami chiqmasligi kerak");
        output.Should().NotContain("sardorbek.gulomjonov@salohiyat-maktab.uz", "email chiqmasligi kerak");
        output.Should().NotContain("2010-05-20", "aniq tug'ilgan sana (ISO) chiqmasligi kerak");
        output.Should().NotContain("20.05.2010", "aniq tug'ilgan sana (mahalliy format) chiqmasligi kerak");

        // Ruxsat etilgan yagona "shaxsiy" maydon — BUTUN SON yosh (`docs/09` §3, `CLAUDE.md` 5-qoida).
        using var document = JsonDocument.Parse(result.AnalysisInputJson);
        var ageElement = document.RootElement.GetProperty("context").GetProperty("age");
        ageElement.ValueKind.Should().Be(JsonValueKind.Number);
        ageElement.TryGetInt32(out _).Should().BeTrue("yosh butun son ('17') sifatida chiqishi kerak, tug'ilgan sana emas");
    }

    // --- AnalysisInput tuzilishi ------------------------------------------------------------

    [Fact]
    public async Task BuildAsync_ComputesIntegerAgeFromBirthDate()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using (var setup = NewContext(connection))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        // Tug'ilgan kun 2026-03-10 (Now) dan KEYIN — demak yosh hali bir yil "to'lmagan".
        var student = CreateStudent(birthDate: new DateOnly(2010, 6, 1));
        var assessment = CreateAssessment();

        await using var context = NewContext(connection);
        var builder = new PromptBuilder(context, new EfAsyncQueryExecutor());

        var result = await builder.BuildAsync(student, assessment, [], Now);

        using var document = JsonDocument.Parse(result.AnalysisInputJson);
        document.RootElement.GetProperty("context").GetProperty("age").GetInt32().Should().Be(15);
        document.RootElement.GetProperty("context").GetProperty("grade").GetInt32().Should().Be(9);
        document.RootElement.GetProperty("context").GetProperty("gender").GetString().Should().Be("male");
        document.RootElement.GetProperty("context").GetProperty("language").GetString().Should().Be("uz");
    }

    [Fact]
    public async Task BuildAsync_WithAllFourSystemTests_BuildsFullAnalysisInput()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using (var setup = NewContext(connection))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.TypeCatalog.Add(TypeCatalogEntry.Create("INTJ", "Loyihachi", "Qisqa tavsif", "Uzun tavsif"));
            setup.CareerMap.Add(CareerMapEntry.Create(Guid.NewGuid(), "IR", "Muhandislik", 1));
            setup.CareerMap.Add(CareerMapEntry.Create(Guid.NewGuid(), "IA", "IT", 2));
            await setup.SaveChangesAsync();
        }

        var student = CreateStudent();
        var assessment = CreateAssessment();
        var testResults = new List<TestResult>
        {
            Mbti16Result(assessment.Id),
            Big5Result(assessment.Id),
            RiasecResult(assessment.Id),
            ActivityResult(assessment.Id, needsAttention: true),
        };

        await using var context = NewContext(connection);
        var builder = new PromptBuilder(context, new EfAsyncQueryExecutor());

        var result = await builder.BuildAsync(student, assessment, testResults, Now);

        using var document = JsonDocument.Parse(result.AnalysisInputJson);
        var root = document.RootElement;

        root.GetProperty("reliability").GetProperty("score").GetDouble().Should().Be(82.5);
        root.GetProperty("reliability").GetProperty("flag").GetString().Should().Be("Reliable");

        var personality16 = root.GetProperty("personality16");
        personality16.GetProperty("type").GetString().Should().Be("INTJ");
        personality16.GetProperty("typeNameUz").GetString().Should().Be("Loyihachi");
        personality16.GetProperty("axes").GetProperty("TF").GetProperty("letter").GetString().Should().Be("T");
        personality16.GetProperty("borderlineAxes").EnumerateArray().Select(e => e.GetString()).Should().ContainSingle(a => a == "TF");

        var bigFive = root.GetProperty("bigFive");
        bigFive.GetProperty("O").GetProperty("pct").GetDouble().Should().Be(70);
        bigFive.GetProperty("O").GetProperty("level").GetString().Should().Be("Yuqori");
        bigFive.GetProperty("stability").GetProperty("pct").GetDouble().Should().Be(70);
        bigFive.GetProperty("stability").GetProperty("level").GetString().Should().Be("Yuqori");
        bigFive.GetProperty("maturityIndex").GetDouble().Should().Be(68.4);
        bigFive.GetProperty("maturityLevel").GetString().Should().Be("Yaxshi");
        bigFive.TryGetProperty("N", out _).Should().BeFalse("`N` qasddan chiqarilmaydi, faqat `stability` (`docs/09` §3)");

        var interests = root.GetProperty("interests");
        interests.GetProperty("hollandCode").GetString().Should().Be("IRA");
        interests.GetProperty("types").GetProperty("I").GetDouble().Should().Be(88);
        interests.GetProperty("consistency").GetString().Should().Be("High");
        interests.GetProperty("mappedFields").EnumerateArray().Select(e => e.GetString()).Should().BeEquivalentTo(["Muhandislik", "IT"]);

        var activity = root.GetProperty("activity");
        activity.GetProperty("MOT").GetDouble().Should().Be(74);
        activity.GetProperty("activityLevel").GetString().Should().Be("Moderate");
        activity.GetProperty("needsAttention").GetBoolean().Should().BeTrue();

        root.GetProperty("customTests").GetArrayLength().Should().Be(0);

        // Prompt matnida placeholder AI JSON bilan almashtirilgan bo'lishi kerak.
        result.UserText.Should().NotContain(DefaultPromptTemplates.AnalysisInputPlaceholder);
        result.UserText.Should().Contain("\"hollandCode\":\"IRA\"");
        result.SystemText.Should().Be(DefaultPromptTemplates.SystemTextV1);
        result.PromptVersion.Should().Be("v1.0");
    }

    [Fact]
    public async Task BuildAsync_WithCustomTest_UsesTestDefinitionNameAndScaleCodesAsLevels()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using (var setup = NewContext(connection))
        {
            await setup.Database.EnsureCreatedAsync();
            var customDefinition = TestDefinition.Create(
                Guid.NewGuid(), "STRESS01", "Stressga chidamlilik anketasi", 10, 5, "SUM", Now);
            setup.TestDefinitions.Add(customDefinition);
            await setup.SaveChangesAsync();
        }

        var student = CreateStudent();
        var assessment = CreateAssessment();
        var testResults = new List<TestResult> { CustomResult(assessment.Id) };

        await using var context = NewContext(connection);
        var builder = new PromptBuilder(context, new EfAsyncQueryExecutor());

        var result = await builder.BuildAsync(student, assessment, testResults, Now);

        using var document = JsonDocument.Parse(result.AnalysisInputJson);
        var customTests = document.RootElement.GetProperty("customTests");
        customTests.GetArrayLength().Should().Be(1);

        var customTest = customTests[0];
        customTest.GetProperty("name").GetString().Should().Be("Stressga chidamlilik anketasi");

        var scales = customTest.GetProperty("scales").EnumerateArray().ToList();
        scales.Should().HaveCount(2);
        scales.Should().Contain(s => s.GetProperty("name").GetString() == "STRESS" && s.GetProperty("pct").GetDouble() == 58 && s.GetProperty("level").GetString() == "O'rtacha");
        scales.Should().Contain(s => s.GetProperty("name").GetString() == "SUPPORT" && s.GetProperty("pct").GetDouble() == 74 && s.GetProperty("level").GetString() == "Yuqori");

        // Personality16/BigFive/Interests/Activity yo'q bo'lganda maydonlar umuman chiqmaydi (null emas).
        document.RootElement.TryGetProperty("personality16", out _).Should().BeFalse();
        document.RootElement.TryGetProperty("bigFive", out _).Should().BeFalse();
    }

    [Fact]
    public async Task BuildAsync_WhenPromptTemplateActiveInDb_UsesDbTemplateInsteadOfEmbeddedDefault()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using (var setup = NewContext(connection))
        {
            await setup.Database.EnsureCreatedAsync();
            var template = PromptTemplate.Create(
                Guid.NewGuid(), "full_analysis", "v1.1", "Maxsus tizim matni.", "Maxsus foydalanuvchi matni: {ANALYSIS_INPUT_JSON}", "{}", Now);
            template.Activate();
            setup.PromptTemplates.Add(template);
            await setup.SaveChangesAsync();
        }

        var student = CreateStudent();
        var assessment = CreateAssessment();

        await using var context = NewContext(connection);
        var builder = new PromptBuilder(context, new EfAsyncQueryExecutor());

        var result = await builder.BuildAsync(student, assessment, [], Now);

        result.PromptVersion.Should().Be("v1.1");
        result.SystemText.Should().Be("Maxsus tizim matni.");
        result.UserText.Should().StartWith("Maxsus foydalanuvchi matni: {");
    }
}
