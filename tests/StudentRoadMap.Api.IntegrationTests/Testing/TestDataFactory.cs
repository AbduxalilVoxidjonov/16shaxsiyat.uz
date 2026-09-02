using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Testing;

/// <summary>Testlar uchun minimal maktab/anketa yaratuvchi yordamchi (haqiqiy domen fabrikalari orqali).</summary>
internal static class TestDataFactory
{
    public static async Task<School> CreateSchoolAsync(
        AppDbContext db,
        DateTimeOffset now,
        string slugSeed,
        string accessToken,
        int dailyRegistrationLimit = 500,
        bool isActive = true,
        string? accessCode = null)
    {
        var slug = SchoolSlug.Create(slugSeed).Value;
        var school = School.Create(
            Guid.NewGuid(),
            $"Maktab {slugSeed}",
            "Toshkent",
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

        return test;
    }

    public static string NewAccessToken(string seed) => $"access-token-{seed}-0123456789abcdef0123456789";
}
