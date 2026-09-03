using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Schools;
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
///
/// <para>
/// ⚠️ 2026-09-03 dan boshlab sessiya HAQIQIY FK zanjiri bilan quriladi
/// (`School → Student → AssessmentProgram → Assessment → AssessmentTest → TestDefinition`):
/// `PromptBuilder` endi qaysi natija shaxsiyat tipi/omillar/qiziqish/aktivlik ekanini
/// metodika KODI bo'yicha emas, `PersonalityBattery.RoleOf` (ya'ni `TestDefinition.Kind` +
/// `ScoringMode` + `ScoringStrategyCode`) bo'yicha aniqlaydi va buning uchun `AssessmentTest`
/// qatorlari bazada BO'LISHI shart.
/// </para>
/// </summary>
public sealed class PromptBuilderTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 10, 0, 0, TimeSpan.Zero);

    /// <summary>Haqiqiy batareya anketasining kodi ATAYLAB strategiya kodiga o'xshamaydi (`docs/06` 8-bo'lim).</summary>
    private const string PersonalityBatteryTypeCode = "PERS-BAT-1";

    private const string PersonalityBatteryTraitsCode = "PERS-BAT-2";

    private const string PersonalityBatteryCareerCode = "PERS-BAT-3";

    private const string PersonalityBatteryActivityCode = "PERS-BAT-4";

    private static AppDbContext NewContext(SqliteConnection connection) =>
        SqliteAppDbContextFactory.CreateContext(connection, new FixedDateTimeProvider(Now), new NoOpPublisher());

    private static PhoneNumber Phone(string raw) => PhoneNumber.Create(raw).Value;

    private static School CreateSchool(string nameSeed = "Test maktabi") => School.Create(
        Guid.NewGuid(),
        nameSeed,
        "Toshkent",
        "Chilonzor",
        SchoolSlug.Create($"maktab-{Guid.NewGuid():N}").Value,
        $"access-{Guid.NewGuid():N}",
        Now);

    private static Student CreateStudent(
        Guid schoolId,
        string fullName = "Alisher Karimov",
        DateOnly? birthDate = null,
        Gender gender = Gender.Male,
        int grade = 9,
        string phone = "901234567",
        string? email = "alisher.karimov@example.com") =>
        Student.Create(
            Guid.NewGuid(),
            schoolId,
            fullName,
            birthDate ?? new DateOnly(2010, 5, 20),
            gender,
            grade,
            Phone(phone),
            Now,
            Now,
            email: email);

    /// <summary>Bazaga saqlanmaydigan sessiya — test blokisiz ssenariylar uchun (yosh/shablon testlari).</summary>
    private static Assessment CreateAssessment(double reliabilityScore = 82.5, ReliabilityFlag flag = ReliabilityFlag.Reliable, string language = "uz")
    {
        var assessment = Assessment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "session-token-xyz", language, Guid.NewGuid(), Now, Now.AddDays(7), Now);
        assessment.SetReliability(reliabilityScore, flag, Now);
        return assessment;
    }

    /// <summary>Sessiyaga biriktiriladigan bitta anketa tavsifi — KOD va STRATEGIYA ataylab alohida.</summary>
    private sealed record TestBlockSpec(
        string Code,
        string ScoringStrategyCode,
        TestKind Kind = TestKind.Standard,
        TestScoringMode ScoringMode = TestScoringMode.Scored,
        string? NameUz = null);

    private sealed record SeededSession(Assessment Assessment, IReadOnlyDictionary<string, Guid> AssessmentTestIdByCode)
    {
        public Guid AssessmentTestId(string code) => AssessmentTestIdByCode[code];
    }

    /// <summary>
    /// Bazani yaratadi va HAQIQIY FK zanjirini saqlaydi. `PromptBuilder` rol xaritasini
    /// `AssessmentTest → TestDefinition` join'i orqali quradi — shu sabab bu qatorlar
    /// bazada bo'lmasa hech bir batareya bloki topilmaydi.
    /// </summary>
    private static async Task<SeededSession> SeedSessionAsync(
        SqliteConnection connection,
        School school,
        Student student,
        params TestBlockSpec[] blocks)
    {
        await using var setup = NewContext(connection);
        await setup.Database.EnsureCreatedAsync();

        var program = AssessmentProgram.Create(Guid.NewGuid(), $"PROG-{Guid.NewGuid():N}"[..12], "Sinov dasturi", Now);
        var assessment = Assessment.Create(
            Guid.NewGuid(), student.Id, school.Id, $"tok-{Guid.NewGuid():N}", "uz", program.Id, Now, Now.AddDays(7), Now);
        assessment.SetReliability(82.5, ReliabilityFlag.Reliable, Now);

        var assessmentTestIdByCode = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var displayOrder = 1;

        foreach (var block in blocks)
        {
            var definition = TestDefinition.Create(
                Guid.NewGuid(),
                block.Code,
                block.NameUz ?? $"{block.Code} anketasi",
                displayOrder,
                estimatedMinutes: 5,
                scoringStrategyCode: block.ScoringStrategyCode,
                now: Now,
                kind: block.Kind,
                scoringMode: block.ScoringMode);
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

    /// <summary>Faqat baza sxemasi kerak bo'lgan (sessiyasiz) testlar uchun.</summary>
    private static async Task EnsureDatabaseAsync(SqliteConnection connection)
    {
        await using var setup = NewContext(connection);
        await setup.Database.EnsureCreatedAsync();
    }

    private static TestResult Mbti16Result(SeededSession session, string testCode, string resultCode = "INTJ") => TestResult.Create(
        Guid.NewGuid(),
        session.AssessmentTestId(testCode),
        session.Assessment.Id,
        testCode,
        rawScoresJson: "{}",
        normalizedScoresJson: """{"EI":28.3,"SN":71.6,"TF":33.3,"JP":64.1}""",
        scoringVersion: 1,
        testVersion: 1,
        computedAt: Now,
        resultCode: resultCode,
        levelsJson: """{"EI":"I","SN":"N","TF":"T","JP":"J"}""",
        flagsJson: """["Borderline:TF"]""");

    private static TestResult Big5Result(SeededSession session, string testCode) => TestResult.Create(
        Guid.NewGuid(),
        session.AssessmentTestId(testCode),
        session.Assessment.Id,
        testCode,
        rawScoresJson: "{}",
        normalizedScoresJson: """{"O":70,"C":77.5,"E":35,"A":62.5,"N":30,"STABILITY":70}""",
        scoringVersion: 1,
        testVersion: 1,
        computedAt: Now,
        levelsJson: """{"O":"Yuqori","C":"Yuqori","E":"Past","A":"Yuqori","N":"Past","MATURITY":"Yaxshi"}""",
        compositeIndex: 68.4);

    private static TestResult RiasecResult(SeededSession session, string testCode, string resultCode = "IRA") => TestResult.Create(
        Guid.NewGuid(),
        session.AssessmentTestId(testCode),
        session.Assessment.Id,
        testCode,
        rawScoresJson: "{}",
        normalizedScoresJson: """{"R":62,"I":88,"ART":71,"SOC":40,"ENT":35,"CONV":48,"DIFFERENTIATION":53}""",
        scoringVersion: 1,
        testVersion: 1,
        computedAt: Now,
        resultCode: resultCode,
        levelsJson: """{"CONSISTENCY":"High"}""");

    private static TestResult ActivityResult(SeededSession session, string testCode, bool needsAttention = false) => TestResult.Create(
        Guid.NewGuid(),
        session.AssessmentTestId(testCode),
        session.Assessment.Id,
        testCode,
        rawScoresJson: "{}",
        normalizedScoresJson: """{"MOT":74,"SELF":68,"SOCA":52,"ENG":60}""",
        scoringVersion: 1,
        testVersion: 1,
        computedAt: Now,
        levelsJson: """{"ACTIVITY":"Moderate"}""",
        compositeIndex: 65.2,
        flagsJson: needsAttention ? """["NeedsAttention"]""" : "[]");

    private static TestResult CustomResult(SeededSession session, string testCode) => TestResult.Create(
        Guid.NewGuid(),
        session.AssessmentTestId(testCode),
        session.Assessment.Id,
        testCode,
        rawScoresJson: "{}",
        normalizedScoresJson: """{"STRESS":58,"SUPPORT":74}""",
        scoringVersion: 1,
        testVersion: 1,
        computedAt: Now,
        levelsJson: """{"STRESS":"O'rtacha","SUPPORT":"Yuqori"}""");

    /// <summary>
    /// Chalg'ituvchi natija — `MBTI16` KODLI, lekin `Custom` + `SUM` strategiyali superadmin
    /// anketasi. Ballari HAQIQIY batareyanikidan farq qiladi (`ESFP`, boshqa foizlar), shu
    /// sabab u promptga sizib chiqsa assertsiya darhol ushlaydi.
    /// </summary>
    private static TestResult DecoyMbti16CodedCustomResult(SeededSession session) => TestResult.Create(
        Guid.NewGuid(),
        session.AssessmentTestId("MBTI16"),
        session.Assessment.Id,
        "MBTI16",
        rawScoresJson: "{}",
        normalizedScoresJson: """{"EI":91.1,"SN":12.2,"TF":93.3,"JP":14.4}""",
        scoringVersion: 1,
        testVersion: 1,
        computedAt: Now,
        resultCode: "ESFP",
        levelsJson: """{"EI":"E","SN":"S","TF":"F","JP":"P"}""",
        flagsJson: "[]");

    // --- MAJBURIY: shaxsiy ma'lumot chiqishda yo'qligi ------------------------------------

    [Fact]
    public async Task BuildAsync_OutputNeverContainsStudentPersonalIdentifiers()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();

        // Haqiqiy PII bilan to'ldirilgan Student — F.I.Sh., telefon, email, aniq tug'ilgan sana.
        // Maktab nomi ham ataylab "tanib olinadigan" qilingan.
        var school = CreateSchool("Shaxsiyat maktabi (maxfiy nom)");
        var student = CreateStudent(
            school.Id,
            fullName: "G'ulomjonov Sardorbek Baxtiyor o'g'li",
            birthDate: new DateOnly(2010, 5, 20),
            phone: "907654321",
            email: "sardorbek.gulomjonov@shaxsiyat-maktab.uz");

        var session = await SeedSessionAsync(
            connection,
            school,
            student,
            new TestBlockSpec(PersonalityBatteryTypeCode, "MBTI16"),
            new TestBlockSpec(PersonalityBatteryTraitsCode, "BIG5"),
            new TestBlockSpec(PersonalityBatteryCareerCode, "RIASEC"),
            new TestBlockSpec(PersonalityBatteryActivityCode, "ACTIVITY"));

        var testResults = new List<TestResult>
        {
            Mbti16Result(session, PersonalityBatteryTypeCode),
            Big5Result(session, PersonalityBatteryTraitsCode),
            RiasecResult(session, PersonalityBatteryCareerCode),
            ActivityResult(session, PersonalityBatteryActivityCode),
        };

        await using var context = NewContext(connection);
        var builder = new PromptBuilder(context, new EfAsyncQueryExecutor());

        var result = await builder.BuildAsync(student, session.Assessment, testResults, Now);

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
        output.Should().NotContain("sardorbek.gulomjonov@shaxsiyat-maktab.uz", "email chiqmasligi kerak");
        output.Should().NotContain("2010-05-20", "aniq tug'ilgan sana (ISO) chiqmasligi kerak");
        output.Should().NotContain("20.05.2010", "aniq tug'ilgan sana (mahalliy format) chiqmasligi kerak");
        output.Should().NotContain("Shaxsiyat maktabi (maxfiy nom)", "maktab nomi chiqmasligi kerak");

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
        await EnsureDatabaseAsync(connection);

        // Tug'ilgan kun 2026-03-10 (Now) dan KEYIN — demak yosh hali bir yil "to'lmagan".
        var student = CreateStudent(Guid.NewGuid(), birthDate: new DateOnly(2010, 6, 1));
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

        var school = CreateSchool();
        var student = CreateStudent(school.Id);

        // ⚠️ Anketa KODLARI strategiya kodlariga ATAYLAB o'xshamaydi (`PERS-BAT-*`) — rol faqat
        // `ScoringStrategyCode`dan kelib chiqishi shu bilan qulflanadi.
        var session = await SeedSessionAsync(
            connection,
            school,
            student,
            new TestBlockSpec(PersonalityBatteryTypeCode, "MBTI16"),
            new TestBlockSpec(PersonalityBatteryTraitsCode, "BIG5"),
            new TestBlockSpec(PersonalityBatteryCareerCode, "RIASEC"),
            new TestBlockSpec(PersonalityBatteryActivityCode, "ACTIVITY"));

        await using (var catalog = NewContext(connection))
        {
            catalog.TypeCatalog.Add(TypeCatalogEntry.Create("INTJ", "Loyihachi", "Qisqa tavsif", "Uzun tavsif"));
            catalog.CareerMap.Add(CareerMapEntry.Create(Guid.NewGuid(), "IR", "Muhandislik", 1));
            catalog.CareerMap.Add(CareerMapEntry.Create(Guid.NewGuid(), "IA", "IT", 2));
            await catalog.SaveChangesAsync();
        }

        var testResults = new List<TestResult>
        {
            Mbti16Result(session, PersonalityBatteryTypeCode),
            Big5Result(session, PersonalityBatteryTraitsCode),
            RiasecResult(session, PersonalityBatteryCareerCode),
            ActivityResult(session, PersonalityBatteryActivityCode, needsAttention: true),
        };

        await using var context = NewContext(connection);
        var builder = new PromptBuilder(context, new EfAsyncQueryExecutor());

        var result = await builder.BuildAsync(student, session.Assessment, testResults, Now);

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

    // --- ⚠️ ZID KODLAR: rol `ScoringStrategyCode` bo'yicha, anketa kodi bo'yicha EMAS ---------

    /// <summary>
    /// `docs/06` 8-bo'lim, 2026-09-03 topilmasi. Sessiyada IKKALASI ham bor:
    /// haqiqiy batareya (`PERS-BAT-1` kodli, `Standard`+`Scored`+`MBTI16` strategiyali) va
    /// chalg'ituvchi `MBTI16` KODLI `Custom` (`SUM`) superadmin anketasi.
    ///
    /// <para>
    /// Eski kod (`testResults.FirstOrDefault(r => r.TestCode == "MBTI16")`) chalg'ituvchini
    /// tanlardi — AI promptida `personality16.type` `"ESFP"` bo'lardi, haqiqiy batareya esa
    /// `customTests`ga tushardi. Ya'ni o'quvchi NOTO'G'RI tahlil olardi va hech qanday xato
    /// ko'rinmasdi.
    /// </para>
    /// </summary>
    [Fact]
    public async Task BuildAsync_KodiZidBolganda_Personality16HaqiqiyBatareyadanOlinadi()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();

        var school = CreateSchool();
        var student = CreateStudent(school.Id);

        var session = await SeedSessionAsync(
            connection,
            school,
            student,
            new TestBlockSpec(PersonalityBatteryTypeCode, "MBTI16"),
            new TestBlockSpec(
                "MBTI16",
                "SUM",
                Kind: TestKind.Custom,
                ScoringMode: TestScoringMode.Scored,
                NameUz: "Superadmin anketasi (MBTI16 kodli)"));

        await using (var catalog = NewContext(connection))
        {
            catalog.TypeCatalog.Add(TypeCatalogEntry.Create("INTJ", "Loyihachi", "Qisqa tavsif", "Uzun tavsif"));
            catalog.TypeCatalog.Add(TypeCatalogEntry.Create("ESFP", "Chalg'ituvchi tip", "Bu tip promptga tushmasligi kerak", "Bu tavsif ham promptga tushmasligi kerak"));
            await catalog.SaveChangesAsync();
        }

        var testResults = new List<TestResult>
        {
            DecoyMbti16CodedCustomResult(session),
            Mbti16Result(session, PersonalityBatteryTypeCode),
        };

        await using var context = NewContext(connection);
        var builder = new PromptBuilder(context, new EfAsyncQueryExecutor());

        var result = await builder.BuildAsync(student, session.Assessment, testResults, Now);

        using var document = JsonDocument.Parse(result.AnalysisInputJson);
        var root = document.RootElement;

        var personality16 = root.GetProperty("personality16");
        personality16.GetProperty("type").GetString().Should().Be(
            "INTJ",
            "shaxsiyat tipi `MBTI16` STRATEGIYASI bilan ballangan `PERS-BAT-1` anketasidan olinadi, kodi `MBTI16` bo'lgan `Custom` anketadan emas");
        personality16.GetProperty("typeNameUz").GetString().Should().Be("Loyihachi");
        personality16.GetProperty("axes").GetProperty("EI").GetProperty("pct").GetDouble().Should().Be(28.3);

        // Chalg'ituvchi anketa — `customTests`da (ya'ni "yo'qolmaydi", lekin tip sifatida talqin qilinmaydi).
        var customTests = root.GetProperty("customTests").EnumerateArray().ToList();
        customTests.Should().ContainSingle();
        customTests[0].GetProperty("name").GetString().Should().Be("Superadmin anketasi (MBTI16 kodli)");

        // ⚠️ ENG MUHIM: noto'g'ri blok PROMPT MATNIGA ham tushmasligi kerak.
        result.UserText.Should().NotContain("ESFP", "chalg'ituvchi anketaning natija kodi AI promptiga tushmasligi kerak");
        result.UserText.Should().NotContain("Chalg'ituvchi tip");
        // `personality16` bloki AYNAN haqiqiy batareyadan qurilgan bo'lishi kerak. (Chalg'ituvchi
        // anketaning foizlari promptda BOR — lekin faqat `customTests` ichida, superadmin
        // anketasi sifatida; ular shaxsiyat tipi talqiniga aylanmaydi.)
        result.UserText.Should().Contain(
            "\"personality16\":{\"type\":\"INTJ\",\"typeNameUz\":\"Loyihachi\",\"axes\":{\"EI\":{\"pct\":28.3",
            "AI promptidagi shaxsiyat bloki `MBTI16` STRATEGIYALI anketadan qurilishi kerak");
    }

    /// <summary>
    /// Batareyasiz dastur (`hasPersonalityBattery = false`): promptda `personality16`/`bigFive`/
    /// `interests`/`activity` bo'limlari UMUMAN bo'lmasligi kerak — bo'sh yoki `0` qiymatli blok
    /// EMAS. `0` va "ma'lumot yo'q" bir xil emas (`docs/06` 8-bo'lim).
    ///
    /// <para>
    /// Eski kodda sessiyadagi `MBTI16` KODLI `Custom` anketa aynan shu bo'lmasligi kerak bo'lgan
    /// `personality16` blokini yaratib qo'yardi.
    /// </para>
    /// </summary>
    [Fact]
    public async Task BuildAsync_BatareyasizDastur_BatareyaBloklariPromptgaUmumanTushmaydi()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();

        var school = CreateSchool();
        var student = CreateStudent(school.Id);

        var session = await SeedSessionAsync(
            connection,
            school,
            student,
            new TestBlockSpec(
                "MBTI16",
                "SUM",
                Kind: TestKind.Custom,
                ScoringMode: TestScoringMode.Scored,
                NameUz: "Superadmin anketasi (MBTI16 kodli)"));

        var testResults = new List<TestResult> { DecoyMbti16CodedCustomResult(session) };

        await using var context = NewContext(connection);
        var builder = new PromptBuilder(context, new EfAsyncQueryExecutor());

        var result = await builder.BuildAsync(student, session.Assessment, testResults, Now);

        using var document = JsonDocument.Parse(result.AnalysisInputJson);
        var root = document.RootElement;

        root.TryGetProperty("personality16", out _).Should().BeFalse("batareya yo'q — bo'lim umuman tushmasligi kerak");
        root.TryGetProperty("bigFive", out _).Should().BeFalse();
        root.TryGetProperty("interests", out _).Should().BeFalse();
        root.TryGetProperty("activity", out _).Should().BeFalse();

        result.UserText.Should().NotContain("personality16");
        result.UserText.Should().NotContain("bigFive");
        result.UserText.Should().NotContain("interests");
        result.UserText.Should().NotContain("ESFP");

        // Natija YO'QOLMAYDI — u superadmin anketasi sifatida `customTests`da qoladi.
        root.GetProperty("customTests").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task BuildAsync_WithCustomTest_UsesTestDefinitionNameAndScaleCodesAsLevels()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();

        var school = CreateSchool();
        var student = CreateStudent(school.Id);

        var session = await SeedSessionAsync(
            connection,
            school,
            student,
            new TestBlockSpec(
                "STRESS01",
                "SUM",
                Kind: TestKind.Custom,
                ScoringMode: TestScoringMode.Scored,
                NameUz: "Stressga chidamlilik anketasi"));

        var testResults = new List<TestResult> { CustomResult(session, "STRESS01") };

        await using var context = NewContext(connection);
        var builder = new PromptBuilder(context, new EfAsyncQueryExecutor());

        var result = await builder.BuildAsync(student, session.Assessment, testResults, Now);

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

        var student = CreateStudent(Guid.NewGuid());
        var assessment = CreateAssessment();

        await using var context = NewContext(connection);
        var builder = new PromptBuilder(context, new EfAsyncQueryExecutor());

        var result = await builder.BuildAsync(student, assessment, [], Now);

        result.PromptVersion.Should().Be("v1.1");
        result.SystemText.Should().Be("Maxsus tizim matni.");
        result.UserText.Should().StartWith("Maxsus foydalanuvchi matni: {");
    }
}
