using Microsoft.EntityFrameworkCore;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Catalog.Branching;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Testing;

/// <summary>Testlar uchun minimal maktab/anketa yaratuvchi yordamchi (haqiqiy domen fabrikalari orqali).</summary>
internal static class TestDataFactory
{
    /// <summary>
    /// P34: `assessments.program_id` endi MAJBURIY FK (`assessment_programs`ga). Mavjud testlar
    /// (P01–P33) dastur tushunchasini bilmaydi — shu sabab `CreatePublishedTestAsync`/
    /// `CreatePublishedRiasecShapedTestWithOptionalExtrasAsync` yaratgan HAR bir test shu YAGONA
    /// standart dasturga avtomatik biriktiriladi (bitta fixture ichida bir nechta chaqiruv bo'lsa
    /// ham — bitta umumiy, `Public`/`Published` dastur). Natijada: bitta fixture'da 1 marta
    /// chaqirilsa — "bitta dastur → avtomatik tanlanadi" ssenariysi (`prompts/34` DoD); bir necha
    /// marta chaqirilsa — barchasi bitta dasturga to'planadi, ya'ni ESKI xatti-harakat (har bir
    /// nashr qilingan test HAR bir sessiyaga avtomatik qo'shilardi) saqlanib qoladi — mavjud
    /// (P01–P33) testlar o'zgarishsiz yashil qoladi.
    /// </summary>
    /// <summary>
    /// Ochiq (public) — bir fixture ichida HAM `CreatePublishedTestAsync` (bu dastur), HAM
    /// haqiqiy `DbSeeder.SeedAsync()` (`PERSONALITY_PROFILE` tizim dasturi) chaqirilgan
    /// aralash holatlarda (masalan bitta sinf ichida bir nechta `[Fact]`) ikkalasi ham
    /// "mavjud dastur" bo'lib qolishi mumkin — bunday testda `StartSessionCommand.ProgramCode`
    /// sifatida ANIQ shu qiymat uzatilib, noaniqlik (`400 PROGRAM_REQUIRED`) oldi olinadi.
    /// </summary>
    public const string DefaultProgramCode = "TEST-DEFAULT-PROGRAM";

    /// <summary>
    /// Faqat `assessments.program_id` FK'ini qondirish uchun (dastur mazmuni ahamiyatsiz bo'lgan
    /// admin/CRUD testlarida) — mavjud bo'lsa qaytaradi, aks holda bo'sh (`Draft`) dastur yaratadi.
    /// Ataylab `AsNoTracking` + yangi (tracking'siz) `Add` — bir nechta fixture chaqiruvi bir xil
    /// `AppDbContext`da bir-birining kuzatilayotgan (tracked) `AssessmentProgram` nusxasi bilan
    /// to'qnashmasligi uchun (`AttachTestToDefaultProgramAsync` bilan bir xil naqsh).
    /// </summary>
    public static async Task<Guid> GetOrCreateDefaultProgramIdAsync(AppDbContext db, DateTimeOffset now)
    {
        var existingId = await db.AssessmentPrograms.AsNoTracking()
            .Where(p => p.Code == DefaultProgramCode)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync();

        if (existingId is not null)
        {
            return existingId.Value;
        }

        var program = AssessmentProgram.Create(Guid.NewGuid(), DefaultProgramCode, "Standart dastur (sinov)", now, visibility: ProgramVisibility.Public);
        db.AssessmentPrograms.Add(program);
        await db.SaveChangesAsync();
        db.Entry(program).State = EntityState.Detached;

        return program.Id;
    }

    /// <summary>
    /// Yangi test blokini standart dasturga qo'shadi va (hali nashr qilinmagan bo'lsa) nashr
    /// qiladi. Butun agregatni (`Include(p => p.Tests)`) qayta yuklab domen metodi orqali
    /// o'zgartirish O'RNIGA to'g'ridan-to'g'ri `ProgramTest` qo'shiladi/`status` yangilanadi —
    /// fixture'lar orasida bo'lishilgan `AppDbContext`da bir xil agregatning ikki marta tracked
    /// holda yuklanishi EF Core konkurrentlik xatosini (`DbUpdateConcurrencyException`,
    /// "0 rows affected") berishi kuzatildi (sinov kodi uchun xavfsiz yechim — ishlab chiqarish
    /// kodida `AssessmentProgram.AddTest`/`Publish` domen metodlari orqali ishlatiladi).
    /// </summary>
    /// <summary>
    /// `public` (avval `private`) — P52 integratsiya testi (`AdminCatalogBranchingImportEndpointTests`,
    /// namunaviy so'rovnoma `SeedDataLoader` orqali to'g'ridan-to'g'ri quriladi, ya'ni bu
    /// yordamchi fabrikalar ORQALI emas) uchun tashqaridan chaqiriladi.
    /// </summary>
    public static async Task AttachTestToDefaultProgramAsync(AppDbContext db, DateTimeOffset now, Guid testDefinitionId, int displayOrder)
    {
        var existing = await db.AssessmentPrograms.AsNoTracking()
            .Where(p => p.Code == DefaultProgramCode)
            .Select(p => new { p.Id, p.Status })
            .FirstOrDefaultAsync();

        if (existing is null)
        {
            var program = AssessmentProgram.Create(Guid.NewGuid(), DefaultProgramCode, "Standart dastur (sinov)", now, visibility: ProgramVisibility.Public);
            program.AddTest(testDefinitionId, displayOrder, now);
            program.Publish(now);
            db.AssessmentPrograms.Add(program);
            await db.SaveChangesAsync();
            db.Entry(program).State = EntityState.Detached;
            return;
        }

        var alreadyLinked = await db.ProgramTests.AsNoTracking()
            .AnyAsync(pt => pt.ProgramId == existing.Id && pt.TestDefinitionId == testDefinitionId);

        if (!alreadyLinked)
        {
            var programTest = ProgramTest.Create(Guid.NewGuid(), existing.Id, testDefinitionId, displayOrder);
            db.ProgramTests.Add(programTest);
            await db.SaveChangesAsync();
            db.Entry(programTest).State = EntityState.Detached;
        }

        if (existing.Status == ProgramStatus.Draft)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE assessment_programs SET status = {(short)ProgramStatus.Published} WHERE id = {existing.Id}");
        }
    }

    /// <summary>
    /// Sinov maktabi. `showResultToStudent` — P47da qo'shildi: natija ko'rsatish endi MAKON
    /// darajasidagi qaror (`School.ShowResultToStudent`, maktab uchun standart `false`) va
    /// global bayroq bilan `&&` qilinadi (`ShowResultPolicy`). Standart qiymat domendagi
    /// bilan bir xil (`false`) — natijani ko'rishni sinaydigan testlar `true` uzatadi.
    /// </summary>
    /// <summary>
    /// Sinov uchun UNIKAL maktab kodi (`SchoolEntryCode` formati) — `ux_schools_entry_code`
    /// SQLite'da ham kuchda, bitta fixture'da ko'p maktab yaratiladi. Ishlab chiqarish
    /// generatori (`Infrastructure.Security.EntryCodeGenerator`) `internal` — shu sabab
    /// bu yerda o'z mini-nusxasi (bir xil alifbo/uzunlik, `RandomNumberGenerator`).
    /// </summary>
    public static string NewEntryCode()
    {
        var chars = new char[SchoolEntryCode.Length];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = SchoolEntryCode.Alphabet[System.Security.Cryptography.RandomNumberGenerator.GetInt32(SchoolEntryCode.Alphabet.Length)];
        }

        return new string(chars);
    }

    public static async Task<School> CreateSchoolAsync(
        AppDbContext db,
        DateTimeOffset now,
        string slugSeed,
        string accessToken,
        int dailyRegistrationLimit = 500,
        bool isActive = true,
        string? accessCode = null,
        string? region = null,
        bool showResultToStudent = false,
        string? entryCode = null)
    {
        var slug = SchoolSlug.Create(slugSeed).Value;
        var school = School.Create(
            Guid.NewGuid(),
            $"Maktab {slugSeed}",
            region ?? "Toshkent",
            "Chilonzor",
            slug,
            accessToken,
            entryCode ?? NewEntryCode(),
            now,
            dailyRegistrationLimit: dailyRegistrationLimit,
            accessCode: accessCode);

        if (showResultToStudent)
        {
            school.SetShowResultToStudent(true, now);
        }

        if (!isActive)
        {
            school.Deactivate(now);
        }

        db.Schools.Add(school);
        await db.SaveChangesAsync();

        return school;
    }

    /// <summary>
    /// Bitta nashr qilingan, faol anketa — `Custom` (`IsSystem = false`), `questionCount` savol
    /// bilan. `requiredCount` — nechta SAVOL (1-chidan boshlab, tartib bo'yicha) `IsRequired = true`
    /// bo'lishi; standart qiymat "hammasi majburiy" (mavjud testlarning aksariyati shunga tayanadi).
    /// Qolgan `questionCount - requiredCount` savol `IsRequired = false` (ixtiyoriy) — QA
    /// tuzatmasi (`prompts/12`): `CompleteTest` faqat majburiy savollarni talab qilishini sinash
    /// uchun.
    /// </summary>
    public static async Task<TestDefinition> CreatePublishedTestAsync(
        AppDbContext db,
        DateTimeOffset now,
        string code,
        int displayOrder,
        int questionCount = 2,
        int pageSize = 10,
        bool shuffleQuestions = false,
        int requiredCount = int.MaxValue)
    {
        var testId = Guid.NewGuid();
        var test = TestDefinition.Create(
            testId,
            code,
            $"{code} nomi",
            displayOrder,
            estimatedMinutes: 5,
            scoringStrategyCode: "SUM",
            now: now,
            pageSize: pageSize,
            shuffleQuestions: shuffleQuestions);

        for (var i = 1; i <= questionCount; i++)
        {
            var question = Question.Create(
                Guid.NewGuid(),
                testId,
                $"{code}-Q{i:00}",
                i,
                $"{code} savoli {i}",
                QuestionType.Likert5,
                "GEN",
                scaleDirection: 1,
                weight: 1.0m,
                isRequired: i <= requiredCount);

            test.AddQuestion(question, now);
        }

        test.Publish(now);

        db.TestDefinitions.Add(test);
        await db.SaveChangesAsync();

        await AttachTestToDefaultProgramAsync(db, now, testId, displayOrder);

        return test;
    }

    /// <summary>
    /// `RIASEC` strategiyasi talab qiladigan ANIQ tuzilishda (6 tip × 8 savol, hammasi
    /// majburiy, `direction=+1`, `docs/03` §4.1) anketa + ustiga `extraOptionalCount` ta
    /// IXTIYORIY ("FILLER" shkalali — `RiasecStrategy` bu shkalani butunlay e'tiborsiz
    /// qoldiradi, chunki faqat `docs/03` §4.1 dagi 6 tipni biladi) savol qo'shadi.
    /// `CompleteTest`ning ixtiyoriy savolni bloklamasligini HAQIQIY (muvaffaqiyatli scoring
    /// bilan) integratsiya darajasida sinash uchun (`prompts/12` QA tuzatmasi) — `SUM`
    /// strategiyasi bu maqsadga yaramaydi, chunki `InterpretationBands` talab qiladi
    /// (`TestScale` entity hali P33'da yo'q), `RIASEC` esa bandssiz ishlaydi.
    ///
    /// <para>
    /// `kind` — sukut bo'yicha `Custom`, ya'ni anketa `RIASEC` STRATEGIYASI bilan ballansa ham
    /// shaxsiyat batareyasiga KIRMAYDI (`PersonalityBattery.Includes`: `Standard` + `Scored`).
    /// Admin javobidagi `results.RIASEC` bloki to'lishi kerak bo'lgan testlar `TestKind.Standard`
    /// uzatadi — 2026-09-03 dan buyon bu blok metodika KODI emas, ROL bo'yicha to'ldiriladi
    /// (`StudentProfileMapping.BuildTestResultsAsync`).
    /// </para>
    /// </summary>
    public static async Task<TestDefinition> CreatePublishedRiasecShapedTestWithOptionalExtrasAsync(
        AppDbContext db,
        DateTimeOffset now,
        string code,
        int displayOrder,
        int extraOptionalCount = 2,
        TestKind kind = TestKind.Custom)
    {
        var testId = Guid.NewGuid();
        var test = TestDefinition.Create(
            testId,
            code,
            $"{code} nomi",
            displayOrder,
            estimatedMinutes: 5,
            scoringStrategyCode: "RIASEC",
            now: now,
            kind: kind,
            pageSize: 60);

        var types = new[] { "R", "I", "ART", "SOC", "ENT", "CONV" };
        var order = 1;
        foreach (var type in types)
        {
            for (var i = 1; i <= 8; i++)
            {
                var question = Question.Create(
                    Guid.NewGuid(),
                    testId,
                    $"{code}-{type}-{i:00}",
                    order++,
                    $"{code} {type} savoli {i}",
                    QuestionType.Likert5,
                    type,
                    scaleDirection: 1,
                    weight: 1.0m,
                    isRequired: true);

                test.AddQuestion(question, now);
            }
        }

        for (var i = 1; i <= extraOptionalCount; i++)
        {
            var question = Question.Create(
                Guid.NewGuid(),
                testId,
                $"{code}-FILLER-{i:00}",
                order++,
                $"{code} ixtiyoriy savol {i}",
                QuestionType.Likert5,
                "FILLER",
                scaleDirection: 1,
                weight: 1.0m,
                isRequired: false);

            test.AddQuestion(question, now);
        }

        test.Publish(now);

        db.TestDefinitions.Add(test);
        await db.SaveChangesAsync();

        await AttachTestToDefaultProgramAsync(db, now, testId, displayOrder);

        return test;
    }

    /// <summary>
    /// `CreatePublishedTestAsync` bilan bir xil, lekin standart dasturga AVTOMATIK
    /// biriktirilmaydi (`prompts/34` dastur tanlash testlari uchun — bir nechta ANIQ nazorat
    /// qilinadigan dastur kerak bo'lganda, masalan `PublicProgramSelectionEndpointTests`).
    /// `scoringMode: Survey` bo'lsa `scoringStrategyCode` e'tiborsiz qoldiriladi (`docs/06` 8-bo'lim).
    /// </summary>
    public static async Task<TestDefinition> CreateStandaloneTestAsync(
        AppDbContext db,
        DateTimeOffset now,
        string code,
        int displayOrder,
        int questionCount = 2,
        StudentRoadMap.Domain.Catalog.TestScoringMode scoringMode = StudentRoadMap.Domain.Catalog.TestScoringMode.Scored,
        string? scoringStrategyCode = "SUM")
    {
        var testId = Guid.NewGuid();
        var test = TestDefinition.Create(
            testId,
            code,
            $"{code} nomi",
            displayOrder,
            estimatedMinutes: 5,
            scoringStrategyCode: scoringMode == StudentRoadMap.Domain.Catalog.TestScoringMode.Survey ? null : scoringStrategyCode,
            now: now,
            scoringMode: scoringMode);

        for (var i = 1; i <= questionCount; i++)
        {
            var question = Question.Create(
                Guid.NewGuid(),
                testId,
                $"{code}-Q{i:00}",
                i,
                $"{code} savoli {i}",
                QuestionType.Likert5,
                "GEN",
                scaleDirection: 1,
                weight: 1.0m,
                isRequired: true);

            test.AddQuestion(question, now);
        }

        test.Publish(now);

        db.TestDefinitions.Add(test);
        await db.SaveChangesAsync();

        return test;
    }

    /// <summary>
    /// `CreatePublishedRiasecShapedTestWithOptionalExtrasAsync` bilan bir xil (6 tip × 8 savol,
    /// `RIASEC` strategiyasi — `SUM` dan farqli, `InterpretationBands` talab qilmaydi), lekin
    /// standart dasturga AVTOMATIK biriktirilmaydi. Yaratilgan anketa `Custom` — ya'ni `code`
    /// sifatida `"BIG5"`/`"MBTI16"` berilsa, "kodi batareyaniki, o'zi batareya emas" degan
    /// CHALG'ITUVCHI holat quriladi (`PublicBig5WithoutActivityEndpointTests`,
    /// `PublicBatteryRoleEndpointTests`). Batareya roli KOD bilan emas,
    /// `PersonalityBattery.RoleOf` bilan aniqlanadi.
    /// </summary>
    public static async Task<TestDefinition> CreateStandaloneRiasecShapedTestAsync(
        AppDbContext db, DateTimeOffset now, string code, int displayOrder)
    {
        var testId = Guid.NewGuid();
        var test = TestDefinition.Create(
            testId, code, $"{code} nomi", displayOrder, estimatedMinutes: 5, scoringStrategyCode: "RIASEC", now: now, pageSize: 60);

        var types = new[] { "R", "I", "ART", "SOC", "ENT", "CONV" };
        var order = 1;
        foreach (var type in types)
        {
            for (var i = 1; i <= 8; i++)
            {
                var question = Question.Create(
                    Guid.NewGuid(), testId, $"{code}-{type}-{i:00}", order++, $"{code} {type} savoli {i}",
                    QuestionType.Likert5, type, scaleDirection: 1, weight: 1.0m, isRequired: true);

                test.AddQuestion(question, now);
            }
        }

        test.Publish(now);

        db.TestDefinitions.Add(test);
        await db.SaveChangesAsync();

        return test;
    }

    /// <summary>
    /// SEED bilan bir xil shaklda (`TestDefinition.CreateSystemPublished`) ilmiy metodika —
    /// `Kind = Standard`, `IsSystem = true`, `ScoringMode = Scored`, darhol `Published`. Ya'ni
    /// `PersonalityBattery` mezoniga TUSHADIGAN yagona haqiqiy shakl (haqiqiy MBTI16/BIG5/
    /// RIASEC/ACTIVITY xuddi shu fabrika orqali seed qilinadi). Standart dasturga AVTOMATIK
    /// biriktirilmaydi — `CreateProgramAsync` bilan aniq dasturga qo'shiladi.
    /// </summary>
    public static async Task<TestDefinition> CreateStandaloneSystemTestAsync(
        AppDbContext db,
        DateTimeOffset now,
        string code,
        int displayOrder,
        int questionCount = 2,
        string scoringStrategyCode = "SUM")
    {
        var testId = Guid.NewGuid();
        var questions = new List<Question>(questionCount);

        for (var i = 1; i <= questionCount; i++)
        {
            questions.Add(Question.Create(
                Guid.NewGuid(),
                testId,
                $"{code}-Q{i:00}",
                i,
                $"{code} savoli {i}",
                QuestionType.Likert5,
                "GEN",
                scaleDirection: 1,
                weight: 1.0m,
                isRequired: true,
                isSystem: true));
        }

        var test = TestDefinition.CreateSystemPublished(
            testId,
            code,
            $"{code} nomi",
            descriptionUz: null,
            displayOrder,
            estimatedMinutes: 5,
            shuffleQuestions: false,
            pageSize: 10,
            scoringStrategyCode,
            questions,
            now);

        db.TestDefinitions.Add(test);
        await db.SaveChangesAsync();

        return test;
    }

    /// <summary>
    /// SEED shaklidagi ilmiy metodika (`Kind = Standard`, `IsSystem`, `Scored`, `Published`),
    /// lekin savollari HAQIQIY scoring uchun yetarli shkalalar bilan quriladi va strategiya kodi
    /// ANIQ beriladi — ya'ni metodika KODI bilan strategiya kodi ATAYLAB har xil bo'lishi mumkin
    /// (masalan `PERS-BAT-1` kodli anketa `MBTI16` strategiyasi bilan). Aynan shu holat
    /// `PersonalityBattery.RoleOf` (rol strategiyadan keladi, koddan emas) qoidasini sinaydi.
    /// </summary>
    /// <param name="scales">`(shkala kodi, savollar soni)` — masalan MBTI16 uchun `EI`/`SN`/`TF`/`JP`.</param>
    public static async Task<TestDefinition> CreateStandaloneSystemScoredTestAsync(
        AppDbContext db,
        DateTimeOffset now,
        string code,
        int displayOrder,
        string scoringStrategyCode,
        IReadOnlyList<(string Scale, int Count)> scales,
        int pageSize = 10)
    {
        var testId = Guid.NewGuid();
        var questions = new List<Question>();
        var order = 1;

        foreach (var (scale, count) in scales)
        {
            for (var i = 1; i <= count; i++)
            {
                questions.Add(Question.Create(
                    Guid.NewGuid(),
                    testId,
                    $"{code}-{scale}-{i:00}",
                    order++,
                    $"{code} {scale} savoli {i}",
                    QuestionType.Likert5,
                    scale,
                    scaleDirection: 1,
                    weight: 1.0m,
                    isRequired: true,
                    isSystem: true));
            }
        }

        var test = TestDefinition.CreateSystemPublished(
            testId,
            code,
            $"{code} nomi",
            descriptionUz: null,
            displayOrder,
            estimatedMinutes: 5,
            shuffleQuestions: false,
            pageSize,
            scoringStrategyCode,
            questions,
            now);

        db.TestDefinitions.Add(test);
        await db.SaveChangesAsync();

        return test;
    }

    /// <summary>`MBTI16` strategiyasi uchun yetarli shkala tarkibi (`EI`/`SN`/`TF`/`JP`) — savol soniga cheklov yo'q (`docs/03` §2.2).</summary>
    public static Task<TestDefinition> CreateStandaloneSystemMbtiShapedTestAsync(
        AppDbContext db, DateTimeOffset now, string code, int displayOrder) =>
        CreateStandaloneSystemScoredTestAsync(
            db, now, code, displayOrder, "MBTI16", [("EI", 2), ("SN", 2), ("TF", 2), ("JP", 2)]);

    /// <summary>`RIASEC` strategiyasi uchun majburiy tarkib — 6 tip × 8 savol (`docs/03` §4.1).</summary>
    public static Task<TestDefinition> CreateStandaloneSystemRiasecShapedTestAsync(
        AppDbContext db, DateTimeOffset now, string code, int displayOrder) =>
        CreateStandaloneSystemScoredTestAsync(
            db, now, code, displayOrder, "RIASEC",
            [("R", 8), ("I", 8), ("ART", 8), ("SOC", 8), ("ENT", 8), ("CONV", 8)],
            pageSize: 60);

    /// <summary>
    /// Yangi (`Custom`, nashr qilingan, `Public` — `visibility` bilan o'zgartiriladi) dastur —
    /// `CreateStandaloneTestAsync` bilan yaratilgan testlarni ANIQ tartibda biriktiradi
    /// (`prompts/34` dastur tanlash testlari uchun).
    /// </summary>
    public static async Task<AssessmentProgram> CreateProgramAsync(
        AppDbContext db,
        DateTimeOffset now,
        string code,
        IReadOnlyList<(Guid TestDefinitionId, int DisplayOrder)> tests,
        ProgramVisibility visibility = ProgramVisibility.Public)
    {
        var program = AssessmentProgram.Create(Guid.NewGuid(), code, $"{code} nomi", now, visibility: visibility);

        foreach (var (testDefinitionId, displayOrder) in tests)
        {
            program.AddTest(testDefinitionId, displayOrder, now);
        }

        program.Publish(now);

        db.AssessmentPrograms.Add(program);
        await db.SaveChangesAsync();

        return program;
    }

    /// <summary>
    /// P52 (`docs/18` §8 "to'liq oqim" integratsiya testi) — minimal `Survey` anketa: filtr
    /// savoli (`{code}-FILTER`, `SingleChoice`, qiymatlar 1/2, majburiy) va unga bog'liq
    /// (`VisibilityRule`) savol (`{code}-FOLLOWUP`, `ShortText`, majburiy, FAQAT
    /// `{code}-FILTER` = 1 bo'lganda ko'rinadi).
    /// </summary>
    public static async Task<TestDefinition> CreatePublishedBranchingFilterSurveyAsync(
        AppDbContext db, DateTimeOffset now, string code, int displayOrder)
    {
        var testId = Guid.NewGuid();
        var test = TestDefinition.Create(
            testId, code, $"{code} nomi", displayOrder, estimatedMinutes: 5,
            scoringStrategyCode: null, now: now, scoringMode: TestScoringMode.Survey, pageSize: 60);

        var filterQuestion = Question.Create(
            Guid.NewGuid(), testId, $"{code}-FILTER", 1, "Qo'shimcha kursga qatnashasizmi?", QuestionType.SingleChoice,
            "SURVEY", scaleDirection: 1, weight: 1.0m, isRequired: true);
        filterQuestion.AddOption(AnswerOption.Create(Guid.NewGuid(), filterQuestion.Id, "Ha", 1, 1));
        filterQuestion.AddOption(AnswerOption.Create(Guid.NewGuid(), filterQuestion.Id, "Yo'q", 2, 2));
        test.AddQuestion(filterQuestion, now);

        var visibility = new VisibilityRule(
            VisibilityMatch.All,
            [new VisibilityCondition($"{code}-FILTER", VisibilityOperator.Equals, [1])]);

        var followUpQuestion = Question.Create(
            Guid.NewGuid(), testId, $"{code}-FOLLOWUP", 2, "Qaysi kurs?", QuestionType.ShortText,
            "SURVEY", scaleDirection: 1, weight: 1.0m, isRequired: true, visibilityRule: visibility);
        test.AddQuestion(followUpQuestion, now);

        test.Publish(now);

        db.TestDefinitions.Add(test);
        await db.SaveChangesAsync();

        await AttachTestToDefaultProgramAsync(db, now, testId, displayOrder);

        return test;
    }

    public static string NewAccessToken(string seed) => $"access-token-{seed}-0123456789abcdef0123456789";
}
