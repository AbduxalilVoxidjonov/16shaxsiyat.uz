using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Catalog;

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
    public static AdminProgramListItemDto ToListItemDto(AssessmentProgram program, int testCount) => new(
        program.Id,
        program.Code,
        program.NameUz,
        program.Kind.ToString(),
        program.Visibility.ToString(),
        program.State.ToString(),
        program.IsSystem,
        program.DisplayOrder,
        testCount);

    /// <summary>
    /// Tarkib (`ProgramTest` + `TestDefinition` proyeksiyasi, `DisplayOrder` bo'yicha DB
    /// darajasida saralangan) va biriktirilgan maktablar ro'yxati bilan to'ldiradi.
    /// </summary>
    public static async Task<AdminProgramDetailDto> BuildDetailDtoAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        AssessmentProgram program,
        CancellationToken cancellationToken)
    {
        var testItems = await executor.ToListAsync(
            context.AsNoTracking(context.ProgramTests)
                .Where(pt => pt.ProgramId == program.Id)
                .OrderBy(pt => pt.DisplayOrder)
                .Join(
                    context.AsNoTracking(context.TestDefinitions),
                    pt => pt.TestDefinitionId,
                    t => t.Id,
                    (pt, t) => new AdminProgramTestItemDto(t.Id, t.Code, t.NameUz, pt.DisplayOrder)),
            cancellationToken).ConfigureAwait(false);

        var assignedSchoolIds = await executor.ToListAsync(
            context.AsNoTracking(context.SchoolPrograms)
                .Where(sp => sp.ProgramId == program.Id)
                .OrderBy(sp => sp.CreatedAt)
                .Select(sp => sp.SchoolId),
            cancellationToken).ConfigureAwait(false);

        return new AdminProgramDetailDto(
            program.Id,
            program.Code,
            program.NameUz,
            program.DescriptionUz,
            program.Kind.ToString(),
            program.Visibility.ToString(),
            program.State.ToString(),
            program.IsSystem,
            program.DisplayOrder,
            testItems,
            assignedSchoolIds,
            program.CreatedAt,
            program.UpdatedAt);
    }
}
