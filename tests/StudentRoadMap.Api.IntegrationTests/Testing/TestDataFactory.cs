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

    /// <summary>Bitta nashr qilingan, faol anketa — `Custom` (`IsSystem = false`), `questionCount` savol bilan.</summary>
    public static async Task<TestDefinition> CreatePublishedTestAsync(
        AppDbContext db,
        DateTimeOffset now,
        string code,
        int displayOrder,
        int questionCount = 2,
        int pageSize = 10,
        bool shuffleQuestions = false)
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
                weight: 1.0m);

            test.AddQuestion(question, now);
        }

        test.Publish(now);

        db.TestDefinitions.Add(test);
        await db.SaveChangesAsync();

        return test;
    }

    public static string NewAccessToken(string seed) => $"access-token-{seed}-0123456789abcdef0123456789";
}
