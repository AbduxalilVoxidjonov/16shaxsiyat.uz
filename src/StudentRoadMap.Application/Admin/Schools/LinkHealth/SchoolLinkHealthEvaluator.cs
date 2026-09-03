using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Admin.Schools.LinkHealth;

/// <summary>
/// Dastur katalogining bir martalik snapshoti — admin tomonidagi "havola ishlaydimi"
/// hisobini N+1 so'rovsiz qilish uchun. Katalog admin tomonidan qo'lda kuratsiya qilinadi
/// (o'nlab yozuv), shu sabab butunlay o'qiladi; maktablar soni bo'yicha SIKL YO'Q.
/// </summary>
/// <param name="Programs">Barcha `assessment_programs` yozuvlari.</param>
/// <param name="ProgramIdsWithUsableTest">
/// Tarkibida kamida bitta YAROQLI test bo'lgan dastur ID'lari. Yaroqli test —
/// `TestDefinition.Status = Published &amp;&amp; IsActive` VA kamida bitta `IsActive` savol
/// (aynan shu shart `GetSchoolInfoQueryHandler.BuildProgramsAsync` va
/// `StartSessionCommandHandler` da testni sessiyaga qo'shishdan oldin qo'llanadi).
/// </param>
internal sealed record ProgramCatalogSnapshot(
    IReadOnlyList<AssessmentProgram> Programs,
    IReadOnlySet<Guid> ProgramIdsWithUsableTest);

/// <summary>
/// Maktab havolasining sog'ligini <b>ommaviy handler bilan AYNAN BIR XIL</b> mezon bo'yicha
/// hisoblaydi — mezon <see cref="ProgramAvailability.Matches"/> da, bitta joyda
/// (`docs/06` §8, 2026-09-02 qarori + 2026-09-03 jonli hodisasi).
///
/// **N+1 YO'Q:** butun hisob uchun 4 ta so'rov (dasturlar, `program_tests`, `test_definitions`,
/// `questions`) + baholanadigan maktablar uchun BITTA `school_programs` so'rovi. Maktab yoki
/// dastur bo'yicha sikl ichida so'rov YO'Q.
/// </summary>
internal static class SchoolLinkHealthEvaluator
{
    public static async Task<ProgramCatalogSnapshot> LoadCatalogAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        CancellationToken cancellationToken)
    {
        var programs = await executor.ToListAsync(
            context.AsNoTracking(context.AssessmentPrograms),
            cancellationToken).ConfigureAwait(false);

        // Faqat `Published && IsActive` dasturlar uchun test tekshiruvi kerak — qolganlari
        // ko'rinishidan qat'i nazar mavjud emas.
        var candidateProgramIds = programs
            .Where(p => p.Status == ProgramStatus.Published && p.IsActive)
            .Select(p => p.Id)
            .ToList();

        if (candidateProgramIds.Count == 0)
        {
            return new ProgramCatalogSnapshot(programs, new HashSet<Guid>());
        }

        var programTestRows = await executor.ToListAsync(
            context.AsNoTracking(context.ProgramTests)
                .Where(pt => candidateProgramIds.Contains(pt.ProgramId))
                .Select(pt => new { pt.ProgramId, pt.TestDefinitionId }),
            cancellationToken).ConfigureAwait(false);

        var testDefinitionIds = programTestRows.Select(r => r.TestDefinitionId).Distinct().ToList();

        var publishedTestDefinitionIds = await executor.ToListAsync(
            context.AsNoTracking(context.TestDefinitions)
                .Where(t => testDefinitionIds.Contains(t.Id) && t.Status == TestDefinitionStatus.Published && t.IsActive)
                .Select(t => t.Id),
            cancellationToken).ConfigureAwait(false);

        var testDefinitionIdsWithQuestions = await executor.ToListAsync(
            context.AsNoTracking(context.Questions)
                .Where(q => publishedTestDefinitionIds.Contains(q.TestDefinitionId) && q.IsActive)
                .Select(q => q.TestDefinitionId)
                .Distinct(),
            cancellationToken).ConfigureAwait(false);

        var usableTestIds = testDefinitionIdsWithQuestions.ToHashSet();

        var programIdsWithUsableTest = programTestRows
            .Where(r => usableTestIds.Contains(r.TestDefinitionId))
            .Select(r => r.ProgramId)
            .ToHashSet();

        return new ProgramCatalogSnapshot(programs, programIdsWithUsableTest);
    }

    /// <summary>
    /// Berilgan maktablar uchun `school_programs` biriktirmalarini BITTA so'rovda o'qiydi.
    /// </summary>
    public static async Task<Dictionary<Guid, HashSet<Guid>>> LoadAssignmentsAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IReadOnlyCollection<Guid> schoolIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, HashSet<Guid>>();
        if (schoolIds.Count == 0)
        {
            return result;
        }

        var rows = await executor.ToListAsync(
            context.AsNoTracking(context.SchoolPrograms)
                .Where(sp => schoolIds.Contains(sp.SchoolId))
                .Select(sp => new { sp.SchoolId, sp.ProgramId }),
            cancellationToken).ConfigureAwait(false);

        foreach (var row in rows)
        {
            if (!result.TryGetValue(row.SchoolId, out var set))
            {
                set = [];
                result[row.SchoolId] = set;
            }

            set.Add(row.ProgramId);
        }

        return result;
    }

    /// <summary>
    /// Bitta maktabning holati. `assignedProgramIds` — shu maktabga biriktirilgan dastur ID'lari
    /// (bo'lmasa bo'sh to'plam).
    /// </summary>
    public static AdminSchoolLinkHealthDto Evaluate(ProgramCatalogSnapshot snapshot, IReadOnlySet<Guid>? assignedProgramIds)
    {
        var assigned = assignedProgramIds ?? new HashSet<Guid>();

        // ————— MEZON: ommaviy `GetSchoolInfoQueryHandler` bilan bitta manbadan —————
        var available = snapshot.Programs
            .Where(p => ProgramAvailability.Matches(p, assigned.Contains(p.Id)))
            .ToList();

        if (available.Count == 0)
        {
            var status = snapshot.Programs.Count == 0
                ? SchoolLinkHealthStatus.NoProgramsAtAll
                : snapshot.Programs.Any(p => p.Visibility == ProgramVisibility.Public || assigned.Contains(p.Id))
                    ? SchoolLinkHealthStatus.ProgramsDeactivated
                    : SchoolLinkHealthStatus.NoProgramAssigned;

            return new AdminSchoolLinkHealthDto(status.ToString(), 0, 0);
        }

        var usableCount = available.Count(p => snapshot.ProgramIdsWithUsableTest.Contains(p.Id));

        return usableCount == 0
            ? new AdminSchoolLinkHealthDto(SchoolLinkHealthStatus.ProgramsWithoutTests.ToString(), available.Count, 0)
            : new AdminSchoolLinkHealthDto(SchoolLinkHealthStatus.Ok.ToString(), available.Count, usableCount);
    }

    /// <summary>Ko'p maktab uchun — katalog va biriktirmalar bir martadan o'qiladi.</summary>
    public static async Task<Dictionary<Guid, AdminSchoolLinkHealthDto>> EvaluateManyAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IReadOnlyCollection<Guid> schoolIds,
        CancellationToken cancellationToken)
    {
        if (schoolIds.Count == 0)
        {
            return [];
        }

        var snapshot = await LoadCatalogAsync(context, executor, cancellationToken).ConfigureAwait(false);
        var assignments = await LoadAssignmentsAsync(context, executor, schoolIds, cancellationToken).ConfigureAwait(false);

        return schoolIds.Distinct().ToDictionary(
            id => id,
            id => Evaluate(snapshot, assignments.GetValueOrDefault(id)));
    }

    /// <summary>Bitta maktab uchun (detal sahifasi/mutatsiya javoblari).</summary>
    public static async Task<AdminSchoolLinkHealthDto> EvaluateOneAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        var map = await EvaluateManyAsync(context, executor, [schoolId], cancellationToken).ConfigureAwait(false);

        return map[schoolId];
    }
}
