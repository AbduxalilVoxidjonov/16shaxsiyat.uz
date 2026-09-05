using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Public.Common;

/// <summary>
/// Yangi ochilgan sessiyaga dastur testlarini biriktiradi va ommaviy javob uchun qisqacha
/// ro'yxatni qaytaradi.
///
/// Mantiq ilgari faqat `StartSessionCommandHandler` ichida edi; P47da AYNAN shu qadam ikkinchi
/// oqimga (`StartPublicSessionCommandHandler`) ham kerak bo'lgani uchun BIR JOYGA ko'chirildi.
/// Ko'chirish paytida hech qanday qoida o'zgartirilmadi — tartib `ProgramTest.DisplayOrder`
/// bo'yicha (`TestDefinition.DisplayOrder` EMAS, `prompts/34` C10-band), faqat
/// `Published &amp;&amp; IsActive` anketalar, faol savoli yo'q anketa tushirib qoldiriladi.
/// </summary>
internal static class AssessmentTestAttacher
{
    public static async Task<IReadOnlyList<PublicTestSummaryDto>> AttachAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        Assessment assessment,
        Guid programId,
        CancellationToken cancellationToken)
    {
        var programTests = await executor.ToListAsync(
            context.ProgramTests.Where(pt => pt.ProgramId == programId).OrderBy(pt => pt.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var programTestDefinitionIds = programTests.Select(pt => pt.TestDefinitionId).ToList();
        var testDefinitionsById = (await executor.ToListAsync(
                context.TestDefinitions
                    .Where(t => programTestDefinitionIds.Contains(t.Id) && t.Status == TestDefinitionStatus.Published && t.IsActive),
                cancellationToken).ConfigureAwait(false))
            .ToDictionary(t => t.Id);

        var tests = new List<PublicTestSummaryDto>(programTests.Count);

        foreach (var programTest in programTests)
        {
            if (!testDefinitionsById.TryGetValue(programTest.TestDefinitionId, out var testDefinition))
            {
                continue;
            }

            var activeQuestionCount = await executor.CountAsync(
                context.Questions.Where(q => q.TestDefinitionId == testDefinition.Id && q.IsActive),
                cancellationToken).ConfigureAwait(false);

            if (activeQuestionCount == 0)
            {
                continue;
            }

            var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testDefinition.Id, programTest.DisplayOrder, activeQuestionCount);
            assessment.AddTest(assessmentTest);

            tests.Add(new PublicTestSummaryDto(
                testDefinition.Code,
                testDefinition.NameUz,
                TestStatus.NotStarted.ToString(),
                0,
                activeQuestionCount,
                programTest.DisplayOrder,
                testDefinition.EstimatedMinutes));
        }

        return tests;
    }
}
