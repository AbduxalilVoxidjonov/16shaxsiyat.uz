using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Application.Admin.Programs;

/// <summary>
/// `AdminProgramDetailDto`/`AdminProgramListItemDto` qurish uchun umumiy mantiq —
/// `SchoolMapping` naqshiga o'xshash (`prompts/34` E15-band).
///
/// Holat bu yerda HISOBLANMAYDI — `AssessmentProgram.State` (ya'ni `ProgramStateRules.Resolve`,
/// domen) o'qiladi. Mezon bitta joyda tursin: mapping'da qayta yozilsa, `Admin/PublicSpace`
/// mapping'i bilan darrov ikkiga bo'linardi.
/// </summary>
internal static class ProgramMapping
{
    public static AdminProgramListItemDto ToListItemDto(AssessmentProgram program, int testCount, bool hasPersonalityBattery) => new(
        program.Id,
        program.Code,
        program.NameUz,
        program.Kind.ToString(),
        program.Visibility.ToString(),
        program.RegistrationMode.ToString(),
        program.State.ToString(),
        program.IsSystem,
        program.DisplayOrder,
        testCount,
        hasPersonalityBattery);

    /// <summary>
    /// Tarkib (`ProgramTest` + `TestDefinition` proyeksiyasi, `DisplayOrder` bo'yicha DB
    /// darajasida saralangan), biriktirilgan maktablar ro'yxati va ommaviy makon bayrog'i
    /// (`IsAssignedToPublicSpace`) bilan to'ldiradi.
    /// </summary>
    public static async Task<AdminProgramDetailDto> BuildDetailDtoAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        AssessmentProgram program,
        CancellationToken cancellationToken)
    {
        // `Kind`/`ScoringMode` shu bitta qo'shilgan (`JOIN`) so'rovda birga olinadi —
        // `HasPersonalityBattery` uchun ALOHIDA so'rov YO'Q (N+1 emas). DTO'ga faqat
        // ko'rinadigan maydonlar chiqadi, `Kind`/`ScoringMode` xotirada `.Any(...)` bilan
        // batareya bayrog'iga aylantiriladi (`PersonalityBattery.Includes`, `docs/06` 8-bo'lim).
        var testRows = await executor.ToListAsync(
            context.AsNoTracking(context.ProgramTests)
                .Where(pt => pt.ProgramId == program.Id)
                .OrderBy(pt => pt.DisplayOrder)
                .Join(
                    context.AsNoTracking(context.TestDefinitions),
                    pt => pt.TestDefinitionId,
                    t => t.Id,
                    (pt, t) => new { t.Id, t.Code, t.NameUz, pt.DisplayOrder, t.Kind, t.ScoringMode }),
            cancellationToken).ConfigureAwait(false);

        var testItems = testRows
            .Select(r => new AdminProgramTestItemDto(r.Id, r.Code, r.NameUz, r.DisplayOrder))
            .ToList();

        var hasPersonalityBattery = testRows.Any(r => PersonalityBattery.Includes(r.Kind, r.ScoringMode));

        // Biriktirmalar makon TURI bilan birga o'qiladi (bitta `JOIN`): ommaviy makon
        // (`Kind = PublicSpace`) ro'yxatga KIRMAYDI — u alohida bayroqqa o'tadi
        // (`AdminProgramDetailDto` izohi). `AdminSchoolScope` bu yerda ataylab ishlatilmaydi:
        // biriktirma OLIB TASHLANMAYDI, faqat ikkiga ajratiladi.
        var assignments = await executor.ToListAsync(
            context.AsNoTracking(context.SchoolPrograms)
                .Where(sp => sp.ProgramId == program.Id)
                .Join(
                    context.AsNoTracking(context.Schools),
                    sp => sp.SchoolId,
                    s => s.Id,
                    (sp, s) => new { sp.SchoolId, s.Kind, sp.CreatedAt })
                .OrderBy(x => x.CreatedAt),
            cancellationToken).ConfigureAwait(false);

        var assignedSchoolIds = assignments
            .Where(a => a.Kind == SchoolKind.School)
            .Select(a => a.SchoolId)
            .ToList();

        var isAssignedToPublicSpace = assignments.Any(a => a.Kind == SchoolKind.PublicSpace);

        return new AdminProgramDetailDto(
            program.Id,
            program.Code,
            program.NameUz,
            program.DescriptionUz,
            program.Kind.ToString(),
            program.Visibility.ToString(),
            program.RegistrationMode.ToString(),
            program.State.ToString(),
            program.IsSystem,
            program.DisplayOrder,
            testItems,
            assignedSchoolIds,
            isAssignedToPublicSpace,
            hasPersonalityBattery,
            program.CreatedAt,
            program.UpdatedAt);
    }
}
