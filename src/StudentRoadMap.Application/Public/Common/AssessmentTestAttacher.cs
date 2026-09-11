using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Assessments;

namespace StudentRoadMap.Application.Public.Common;

/// <summary>
/// Yangi ochilgan sessiyaga dastur testlarini biriktiradi va ommaviy javob uchun qisqacha
/// ro'yxatni qaytaradi.
///
/// Mantiq ilgari faqat `StartSessionCommandHandler` ichida edi; P47da AYNAN shu qadam ikkinchi
/// oqimga (`StartPublicSessionCommandHandler`) ham kerak bo'lgani uchun BIR JOYGA ko'chirildi.
/// Dastur uchun MAVJUD testlarni tanlash mezonining o'zi (P52, jonli hodisadan keyin)
/// `ProgramTestCatalog`ga chiqarildi — `GetSchoolInfoQueryHandler` (kirish ekrani) ham AYNAN
/// shu manbadan foydalanadi, tartib `ProgramTest.DisplayOrder` bo'yicha
/// (`TestDefinition.DisplayOrder` EMAS, `prompts/34` C10-band).
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
        var items = await ProgramTestCatalog.GetTestsAsync(context, executor, programId, cancellationToken).ConfigureAwait(false);

        var tests = new List<PublicTestSummaryDto>(items.Count);

        foreach (var item in items)
        {
            var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, item.TestDefinitionId, item.Order, item.ActiveQuestionCount);
            assessment.AddTest(assessmentTest);

            tests.Add(new PublicTestSummaryDto(
                item.Code,
                item.NameUz,
                TestStatus.NotStarted.ToString(),
                0,
                item.ActiveQuestionCount,
                item.Order,
                item.EstimatedMinutes));
        }

        return tests;
    }
}
