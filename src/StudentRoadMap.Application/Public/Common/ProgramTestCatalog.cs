using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Public.Common;

/// <summary>
/// Bitta dastur (`AssessmentProgram`) uchun MAVJUD test bloklari ro'yxati — kirish ekrani
/// (`GetSchoolInfoQueryHandler`) va sessiya boshlanishi (`AssessmentTestAttacher` orqali
/// `StartSessionCommandHandler`/`StartPublicSessionCommandHandler`) BIR XIL ushbu manbadan
/// foydalanadi.
///
/// **2026-09-11 (jonli hodisa):** ilgari `GetSchoolInfoQueryHandler` kirish ekrani uchun BUTUN
/// katalogdan (`TestDefinitions.Where(Published &amp;&amp; IsActive)`, dastur filtri YO'Q) test
/// ro'yxatini olardi, sessiya esa AYNAN shu dasturga biriktirilgan (`ProgramTest`) testlarni.
/// Natijada: dastur arxivlansa ham, o'sha dasturning testlari kirish ekranida ko'rinishda davom
/// etardi — o'quvchi hech qachon topshirmaydigan testlar ro'yxatini ko'rardi. Shu sabab mezon
/// BITTA joyga chiqarildi — aks holda ikkovi yana ajralib ketishi tabiiy (aynan shundan kelib
/// chiqqan xato).
///
/// Mezon (`prompts/34` C10-band bilan bir xil): <see cref="ProgramTest"/> orqali dasturga
/// biriktirilgan, anketa `Status == Published &amp;&amp; IsActive`, VA kamida bitta faol savoli
/// bor (aks holda o'quvchi bo'sh test bilan qoladi — bu qoida ilgari faqat
/// `AssessmentTestAttacher`da bo'lgan, endi kirish ekraniga ham qo'llanadi: aks holda o'quvchi
/// ekranda ko'rgan test soni sessiyadagidan farq qiladi). Tartib —
/// `ProgramTest.DisplayOrder` (`TestDefinition.DisplayOrder` EMAS).
/// </summary>
internal static class ProgramTestCatalog
{
    public static async Task<IReadOnlyList<ProgramTestCatalogItem>> GetTestsAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        Guid programId,
        CancellationToken cancellationToken)
    {
        var programTests = await executor.ToListAsync(
            context.AsNoTracking(context.ProgramTests).Where(pt => pt.ProgramId == programId).OrderBy(pt => pt.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var programTestDefinitionIds = programTests.Select(pt => pt.TestDefinitionId).ToList();
        var testDefinitionsById = (await executor.ToListAsync(
                context.AsNoTracking(context.TestDefinitions)
                    .Where(t => programTestDefinitionIds.Contains(t.Id) && t.Status == TestDefinitionStatus.Published && t.IsActive),
                cancellationToken).ConfigureAwait(false))
            .ToDictionary(t => t.Id);

        var items = new List<ProgramTestCatalogItem>(programTests.Count);

        foreach (var programTest in programTests)
        {
            if (!testDefinitionsById.TryGetValue(programTest.TestDefinitionId, out var testDefinition))
            {
                continue;
            }

            var activeQuestionCount = await executor.CountAsync(
                context.AsNoTracking(context.Questions).Where(q => q.TestDefinitionId == testDefinition.Id && q.IsActive),
                cancellationToken).ConfigureAwait(false);

            if (activeQuestionCount == 0)
            {
                // Faol savoli 0 bo'lgan anketa tushirib qoldiriladi — o'quvchi bo'sh test bilan qolmasin.
                continue;
            }

            items.Add(new ProgramTestCatalogItem(
                testDefinition.Id,
                testDefinition.Code,
                testDefinition.NameUz,
                testDefinition.EstimatedMinutes,
                programTest.DisplayOrder,
                activeQuestionCount,
                testDefinition.Kind,
                testDefinition.ScoringMode));
        }

        return items;
    }
}

/// <summary>
/// Bitta dastur test blokining umumiy (query+command) ko'rinishi — `ProgramTestCatalog.GetTestsAsync`
/// natijasi. `Kind`/`ScoringMode` — `PersonalityBattery.Includes` uchun (`hasPersonalityBattery`
/// bayrog'i), boshqa maydonlar hech qachon o'quvchi API'siga chiqmaydigan `scale`/`direction`
/// EMAS (`CLAUDE.md` 9-qoida) — faqat batareya a'zoligini aniqlash uchun.
/// </summary>
internal sealed record ProgramTestCatalogItem(
    Guid TestDefinitionId,
    string Code,
    string NameUz,
    int EstimatedMinutes,
    int Order,
    int ActiveQuestionCount,
    TestKind Kind,
    TestScoringMode ScoringMode);
