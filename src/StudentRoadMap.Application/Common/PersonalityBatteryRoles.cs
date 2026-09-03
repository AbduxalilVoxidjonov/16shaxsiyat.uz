using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Common;

/// <summary>
/// Sessiyadagi `TestResult`larni <see cref="PersonalityBatteryRole"/> bo'yicha topish — BUTUN
/// tizim uchun YAGONA joy: ommaviy handler'lar (`GetStudentResultQueryHandler`,
/// `CompleteSessionCommandHandler`), admin handler'i (`RecalculateAssessmentScoresCommandHandler`)
/// va `Infrastructure` (`PromptBuilder`, `FallbackReportBuilder`) shu yordamchidan foydalanadi.
/// Shu sabab u `Public/Common/` dan `Common/` ga ko'chirilgan va `public` — `Infrastructure`
/// `Application` ning `internal` turlarini ko'rmaydi (`InternalsVisibleTo` faqat sinov
/// sborkalariga berilgan).
///
/// <para>
/// <b>Nima uchun alohida yordamchi.</b> `TestResult`da rolni aniqlaydigan maydon YO'Q: unda faqat
/// `TestCode` (anketa yorlig'i) bor. Rol esa `TestDefinition.Kind`/`ScoringMode`/
/// `ScoringStrategyCode` dan kelib chiqadi (`PersonalityBattery.RoleOf`), ya'ni
/// `TestResult → AssessmentTest → TestDefinition` zanjiri kerak. Ilgari handler'lar bu zanjirni
/// o'tmasdan `TestCode == "MBTI16"` kabi satr solishtiruvchi qisqa yo'ldan borardi — bu esa
/// `Custom` dasturda yoki kod versiyalanganda JIMGINA noto'g'ri natija berardi (`docs/06`
/// 8-bo'lim, 2026-09-02 "dastur" qarori).
/// </para>
///
/// <para>
/// Rol xaritasi `AssessmentTest.Id` bo'yicha qaytadi — `TestResult.AssessmentTestId` bilan
/// bevosita mos keladi va chaqiruvchi ALLAQACHON yuklagan (kuzatilayotgan) `TestResult`
/// entity'larini qayta yuklashga majbur qilmaydi (`CompleteSession` ularni o'zgartiradi).
/// </para>
/// </summary>
public static class PersonalityBatteryRoles
{
    /// <summary>
    /// Sessiyaning har bir test bloki uchun batareya roli (`AssessmentTest.Id` → rol). Roli
    /// `None` bo'lgan bloklar xaritaga UMUMAN kirmaydi — "rol topilmadi" va "rol yo'q" bir xil
    /// ma'noni bildiradi va chaqiruvchi ularni tasodifan mos deb topa olmaydi.
    /// </summary>
    public static async Task<IReadOnlyDictionary<Guid, PersonalityBatteryRole>> LoadByAssessmentTestIdAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        Guid assessmentId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(executor);

        var rows = await executor.ToListAsync(
            context.AsNoTracking(context.AssessmentTests)
                .Where(t => t.AssessmentId == assessmentId)
                .Join(
                    context.TestDefinitions,
                    assessmentTest => assessmentTest.TestDefinitionId,
                    testDefinition => testDefinition.Id,
                    (assessmentTest, testDefinition) => new
                    {
                        AssessmentTestId = assessmentTest.Id,
                        testDefinition.Kind,
                        testDefinition.ScoringMode,
                        testDefinition.ScoringStrategyCode,
                    }),
            cancellationToken).ConfigureAwait(false);

        var rolesByAssessmentTestId = new Dictionary<Guid, PersonalityBatteryRole>();

        foreach (var row in rows)
        {
            var role = PersonalityBattery.RoleOf(row.Kind, row.ScoringMode, row.ScoringStrategyCode);
            if (role != PersonalityBatteryRole.None)
            {
                rolesByAssessmentTestId[row.AssessmentTestId] = role;
            }
        }

        return rolesByAssessmentTestId;
    }

    /// <summary>
    /// Berilgan rolga ega natijani topadi (topilmasa `null` — "ma'lumot yo'q" holati odatiy,
    /// xato emas: dasturda batareya bo'lmasligi mumkin).
    /// </summary>
    public static TestResult? FindByRole(
        IEnumerable<TestResult> testResults,
        IReadOnlyDictionary<Guid, PersonalityBatteryRole> rolesByAssessmentTestId,
        PersonalityBatteryRole role)
    {
        ArgumentNullException.ThrowIfNull(testResults);
        ArgumentNullException.ThrowIfNull(rolesByAssessmentTestId);

        if (role == PersonalityBatteryRole.None)
        {
            // `None` — "roli yo'q", uni QIDIRISH mantiqiy xato bo'lardi (xaritada bunday yozuv
            // umuman saqlanmaydi, ya'ni jimgina `null` qaytish chaqiruvchidagi xatoni yashirardi).
            throw new ArgumentOutOfRangeException(nameof(role), "Batareya roli `None` bo'yicha natija qidirilmaydi.");
        }

        return testResults.FirstOrDefault(
            result => rolesByAssessmentTestId.TryGetValue(result.AssessmentTestId, out var resultRole) && resultRole == role);
    }
}
