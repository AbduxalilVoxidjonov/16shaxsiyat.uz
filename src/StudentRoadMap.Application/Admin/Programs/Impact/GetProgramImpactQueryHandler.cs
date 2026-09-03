using MediatR;
using StudentRoadMap.Application.Admin.Schools.LinkHealth;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.Impact;

/// <summary>
/// Read-only. Mezon — ayni `SchoolLinkHealthEvaluator`/`ProgramAvailability` (maktab "dasturli"
/// hisoblanadi, agar unga mavjud VA tarkibida yaroqli test bor dastur ko'rinsa).
///
/// **N+1 YO'Q:** katalog snapshoti (4 so'rov) + ta'sirlangan maktablar `COUNT` va namunasi
/// (`EXISTS` bilan filtrlangan 2 so'rov). Maktab bo'yicha sikl ichida so'rov yo'q.
/// </summary>
internal sealed class GetProgramImpactQueryHandler : IRequestHandler<GetProgramImpactQuery, Result<AdminProgramImpactDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public GetProgramImpactQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<AdminProgramImpactDto>> Handle(GetProgramImpactQuery request, CancellationToken cancellationToken)
    {
        if (!ProgramImpactActions.IsKnown(request.Action))
        {
            return Result.Failure<AdminProgramImpactDto>(new Error(
                ProblemCodes.ValidationError,
                "Noma'lum amal — `deactivate`, `archive` yoki `makeAssigned` bo'lishi kerak."));
        }

        var snapshot = await SchoolLinkHealthEvaluator.LoadCatalogAsync(_context, _executor, cancellationToken).ConfigureAwait(false);

        var target = snapshot.Programs.FirstOrDefault(p => p.Id == request.ProgramId);
        if (target is null)
        {
            return Result.Failure<AdminProgramImpactDto>(new Error(ProblemCodes.NotFound, "Dastur topilmadi."));
        }

        // ————— HOZIRGI holat —————
        var usablePublicBefore = snapshot.Programs.Any(p => IsUsablePublic(snapshot, p));
        var usableAssignedIdsBefore = snapshot.Programs
            .Where(p => IsUsableForAssignedSchool(snapshot, p))
            .Select(p => p.Id)
            .ToList();

        // ————— Amaldan KEYINGI holat —————
        // `deactivate`/`archive` — dastur umuman mavjud bo'lmay qoladi;
        // `makeAssigned` — faqat biriktirilgan maktablarga ko'rinadi (Public bo'lmaydi).
        var usablePublicAfter = snapshot.Programs.Any(p => p.Id != target.Id && IsUsablePublic(snapshot, p));

        var usableAssignedIdsAfter = snapshot.Programs
            .Where(p => p.Id != target.Id && IsUsableForAssignedSchool(snapshot, p))
            .Select(p => p.Id)
            .ToList();

        if (request.Action == ProgramImpactActions.MakeAssigned && IsUsableForAssignedSchool(snapshot, target))
        {
            usableAssignedIdsAfter.Add(target.Id);
        }

        // Amaldan keyin ham `Public` yaroqli dastur qolsa — hech bir maktab dastursiz qolmaydi.
        if (usablePublicAfter)
        {
            return Result.Success(new AdminProgramImpactDto(request.Action, 0, []));
        }

        var schoolPrograms = _context.SchoolPrograms;
        var query = _context.AsNoTracking(_context.Schools).Where(s => s.IsActive);

        // "Hozir dasturi bor" sharti — `Public` yaroqli dastur bo'lsa bu barcha maktab uchun
        // to'g'ri, shu sabab qo'shimcha filtr kerak emas (`docs/06` §8: "ma'lumot yo'q ≠ nol" —
        // ilgari ham dastursiz bo'lgan maktab BU AMAL sababli yo'qotmaydi, hisobga olinmaydi).
        if (!usablePublicBefore)
        {
            query = query.Where(s => schoolPrograms.Any(sp => sp.SchoolId == s.Id && usableAssignedIdsBefore.Contains(sp.ProgramId)));
        }

        query = query.Where(s => !schoolPrograms.Any(sp => sp.SchoolId == s.Id && usableAssignedIdsAfter.Contains(sp.ProgramId)));

        var affectedCount = await _executor.CountAsync(query, cancellationToken).ConfigureAwait(false);
        if (affectedCount == 0)
        {
            return Result.Success(new AdminProgramImpactDto(request.Action, 0, []));
        }

        var sample = await _executor.ToListAsync(
            query.OrderBy(s => s.Name).Take(AdminProgramImpactDto.SampleLimit).Select(s => new { s.Id, s.Name }),
            cancellationToken).ConfigureAwait(false);

        var schools = sample.Select(s => new AdminImpactedSchoolDto(s.Id, s.Name)).ToList();

        return Result.Success(new AdminProgramImpactDto(request.Action, affectedCount, schools));
    }

    /// <summary>Biriktirmasiz ham ko'rinadigan (`Public`) va tarkibida yaroqli testi bor dastur.</summary>
    private static bool IsUsablePublic(ProgramCatalogSnapshot snapshot, AssessmentProgram program) =>
        ProgramAvailability.Matches(program, assignedToSchool: false)
        && snapshot.ProgramIdsWithUsableTest.Contains(program.Id);

    /// <summary>Biriktirilgan maktabga ko'rinadigan va tarkibida yaroqli testi bor dastur.</summary>
    private static bool IsUsableForAssignedSchool(ProgramCatalogSnapshot snapshot, AssessmentProgram program) =>
        ProgramAvailability.Matches(program, assignedToSchool: true)
        && snapshot.ProgramIdsWithUsableTest.Contains(program.Id);
}
