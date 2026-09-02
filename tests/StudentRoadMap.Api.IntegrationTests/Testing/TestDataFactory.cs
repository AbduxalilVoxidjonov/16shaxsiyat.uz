using Microsoft.EntityFrameworkCore;
using StudentRoadMap.Domain.Catalog;
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
    private static async Task AttachTestToDefaultProgramAsync(AppDbContext db, DateTimeOffset now, Guid testDefinitionId, int displayOrder)
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

    public static async Task<School> CreateSchoolAsync(
        AppDbContext db,
        DateTimeOffset now,
        string slugSeed,
        string accessToken,
        int dailyRegistrationLimit = 500,
        bool isActive = true,
        string? accessCode = null,
        string? region = null)
    {
        var slug = SchoolSlug.Create(slugSeed).Value;
        var school = School.Create(
            Guid.NewGuid(),
            $"Maktab {slugSeed}",
            region ?? "Toshkent",
            "Chilonzor",
            slug,
            accessToken,
            now,
            dailyRegistrationLimit: dailyRegistrationLimit,
            accessCode: accessCode);

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
    /// </summary>
    public static async Task<TestDefinition> CreatePublishedRiasecShapedTestWithOptionalExtrasAsync(
        AppDbContext db,
        DateTimeOffset now,
        string code,
        int displayOrder,
        int extraOptionalCount = 2)
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
    /// standart dasturga AVTOMATIK biriktirilmaydi. `code` ixtiyoriy — masalan `"BIG5"` berilsa,
    /// `CompleteSessionCommandHandler.ApplyMaturityIndexIfPossible`ning `TestCode == "BIG5"`
    /// tekshiruvini (haqiqiy BIG5 shkalasiz) ishga tushirish uchun ishlatiladi
    /// (`PublicBig5WithoutActivityEndpointTests`).
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

    public static string NewAccessToken(string seed) => $"access-token-{seed}-0123456789abcdef0123456789";
}
