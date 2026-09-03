using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Schools.LinkHealth;

/// <summary>
/// Read-only. **N+1 YO'Q va butun `schools` jadvali xotiraga o'qilmaydi:** katalog snapshoti
/// (4 so'rov) + faol maktablar soni (1 `COUNT`) + "buzuq" maktablar `COUNT` va namunasi
/// (`EXISTS` bilan filtrlangan 2 so'rov) + namunadagi ≤10 maktabning biriktirmalari (1 so'rov).
///
/// **Tez yo'l:** agar `Public` ko'rinishli, nashr qilingan, faol VA yaroqli testi bor dastur
/// bo'lsa — HECH BIR maktab dastursiz qololmaydi (`Public` dastur har qanday maktabga
/// ko'rinadi, `docs/06` §8), shu sabab `0` qaytariladi va maktablar jadvaliga umuman
/// tegilmaydi.
/// </summary>
internal sealed class GetSchoolsLinkHealthQueryHandler
    : IRequestHandler<GetSchoolsLinkHealthQuery, Result<AdminSchoolsLinkHealthDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public GetSchoolsLinkHealthQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<AdminSchoolsLinkHealthDto>> Handle(GetSchoolsLinkHealthQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await SchoolLinkHealthEvaluator.LoadCatalogAsync(_context, _executor, cancellationToken).ConfigureAwait(false);

        var activeSchools = _context.AsNoTracking(_context.Schools).Where(s => s.IsActive);
        var activeSchoolCount = await _executor.CountAsync(activeSchools, cancellationToken).ConfigureAwait(false);

        // `Matches(p, assignedToSchool: false)` — biriktirmadan QAT'I NAZAR ko'rinadigan dastur.
        var hasUsablePublicProgram = snapshot.Programs.Any(p =>
            ProgramAvailability.Matches(p, assignedToSchool: false)
            && snapshot.ProgramIdsWithUsableTest.Contains(p.Id));

        if (hasUsablePublicProgram)
        {
            return Result.Success(new AdminSchoolsLinkHealthDto(activeSchoolCount, 0, []));
        }

        // `Matches(p, assignedToSchool: true)` — biriktirilgan maktabga ko'rinadigan dasturlar
        // (ya'ni `Published && IsActive`, ko'rinishidan qat'i nazar).
        var usableAssignedProgramIds = snapshot.Programs
            .Where(p => ProgramAvailability.Matches(p, assignedToSchool: true)
                && snapshot.ProgramIdsWithUsableTest.Contains(p.Id))
            .Select(p => p.Id)
            .ToList();

        var schoolPrograms = _context.SchoolPrograms;
        var brokenSchools = activeSchools.Where(s =>
            !schoolPrograms.Any(sp => sp.SchoolId == s.Id && usableAssignedProgramIds.Contains(sp.ProgramId)));

        var brokenSchoolCount = await _executor.CountAsync(brokenSchools, cancellationToken).ConfigureAwait(false);

        if (brokenSchoolCount == 0)
        {
            return Result.Success(new AdminSchoolsLinkHealthDto(activeSchoolCount, 0, []));
        }

        var sample = await _executor.ToListAsync(
            brokenSchools
                .OrderBy(s => s.Name)
                .Take(AdminSchoolsLinkHealthDto.SampleLimit)
                .Select(s => new { s.Id, s.Name }),
            cancellationToken).ConfigureAwait(false);

        var sampleIds = sample.Select(s => s.Id).ToList();
        var assignments = await SchoolLinkHealthEvaluator
            .LoadAssignmentsAsync(_context, _executor, sampleIds, cancellationToken)
            .ConfigureAwait(false);

        var items = sample
            .Select(s => new AdminBrokenSchoolLinkDto(
                s.Id,
                s.Name,
                SchoolLinkHealthEvaluator.Evaluate(snapshot, assignments.GetValueOrDefault(s.Id))))
            .ToList();

        return Result.Success(new AdminSchoolsLinkHealthDto(activeSchoolCount, brokenSchoolCount, items));
    }
}
