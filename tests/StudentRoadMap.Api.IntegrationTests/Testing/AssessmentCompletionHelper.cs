using Microsoft.EntityFrameworkCore;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Testing;

/// <summary>
/// Sessiyani API'siz, to'g'ridan-to'g'ri domen metodlari bilan yakunlaydi (`CompletedAt`
/// qo'yiladi) — BR-1 "shu dastur 90 kun ichida yakunlangan" holatini qurish uchun. Har test
/// blokining barcha faol savollariga bir xil javob yoziladi; ballar ahamiyatsiz, faqat
/// `Assessment.Status = Completed` kerak.
/// </summary>
public static class AssessmentCompletionHelper
{
    public static async Task CompleteAsync(AppDbContext db, Guid assessmentId, DateTimeOffset completedAt)
    {
        var assessment = await db.Assessments
            .Include(a => a.Tests)
            .ThenInclude(t => t.Answers)
            .SingleAsync(a => a.Id == assessmentId);

        foreach (var test in assessment.Tests)
        {
            var questionIds = await db.Questions.AsNoTracking()
                .Where(q => q.TestDefinitionId == test.TestDefinitionId && q.IsActive)
                .Select(q => q.Id)
                .ToListAsync();

            assessment.StartTest(test.TestDefinitionId, completedAt);

            var existingAnswerIds = test.Answers.Select(a => a.Id).ToHashSet();

            foreach (var questionId in questionIds)
            {
                test.UpsertAnswer(Guid.NewGuid(), questionId, 3, null, 1000, completedAt);
            }

            // EF Core tuzog'i: navigatsiya orqali topilgan YANGI entity kaliti store-generated
            // (`gen_random_uuid()`) va qiymati berilgan bo'lsa `DetectChanges` uni `Added` emas,
            // `Modified` deb belgilaydi → mavjud bo'lmagan qatorga UPDATE → 0 qator →
            // `DbUpdateConcurrencyException`. Shu sabab yangi javoblar aniq `Added` qilinadi.
            foreach (var answer in test.Answers.Where(a => !existingAnswerIds.Contains(a.Id)))
            {
                db.Entry(answer).State = EntityState.Added;
            }

            assessment.CompleteTest(test.TestDefinitionId, questionIds, completedAt);
        }

        assessment.Complete(completedAt);
        await db.SaveChangesAsync();
    }
}
