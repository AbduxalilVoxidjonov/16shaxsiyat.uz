using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.List;

/// <summary>
/// `prompts/34` E15-band. Read-only — `AsNoTracking`. Test soni ikkinchi (batch) so'rovda —
/// asosiy sahifalash/filtrlash/saralash so'rovi FAQAT `assessment_programs` jadvaliga tegadi
/// (`ListSchoolsQueryHandler` naqshiga o'xshash, `docs/06` §8 P14 qarori: xotirada
/// saralash/agregatsiya taqiqlanadi — saralash DOIM DB darajasida).
/// </summary>
internal sealed class ListProgramsQueryHandler : IRequestHandler<ListProgramsQuery, Result<PagedResult<AdminProgramListItemDto>>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public ListProgramsQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<PagedResult<AdminProgramListItemDto>>> Handle(ListProgramsQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = AdminPagingOptions.Normalize(request.Page, request.PageSize);

        var query = _context.AsNoTracking(_context.AssessmentPrograms);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p => p.NameUz.Contains(term) || p.Code.Contains(term));
        }

        // Hosila holat bo'yicha filtr — shart DB darajasida bajariladi
        // (`ProgramStateRules.Filter`, xotirada filtrlash `docs/06` §8 bo'yicha taqiqlangan).
        // Noma'lum qiymat jimgina e'tiborsiz qoldiriladi — avvalgi `status` filtri bilan
        // bir xil xatti-harakat.
        if (ProgramStateRules.TryParse(request.State, out var state))
        {
            query = query.Where(ProgramStateRules.Filter(state));
        }

        var totalCount = await _executor.CountAsync(query, cancellationToken).ConfigureAwait(false);

        var (sortField, descending) = AdminSortSpec.Parse(request.Sort, defaultField: "displayOrder");
        var sortedQuery = ApplySort(query, sortField, descending);

        var pagePrograms = await _executor.ToListAsync(
            sortedQuery.Skip((page - 1) * pageSize).Take(pageSize),
            cancellationToken).ConfigureAwait(false);

        var programIds = pagePrograms.Select(p => p.Id).ToList();

        // Bitta BATCH so'rov — sahifadagi dasturlar soniga qarab so'rov soni O'SMAYDI (N+1 emas,
        // yuqoridagi sinf izohidagi naqshning davomi). `Kind`/`ScoringMode` shu yerda olinadi va
        // `testCount` bilan bir qatorda `HasPersonalityBattery` (`PersonalityBattery.Includes`)
        // ham xotirada hisoblanadi — ikkinchi so'rov QO'SHILMAYDI.
        var testRows = await _executor.ToListAsync(
            _context.AsNoTracking(_context.ProgramTests)
                .Where(pt => programIds.Contains(pt.ProgramId))
                .Join(
                    _context.AsNoTracking(_context.TestDefinitions),
                    pt => pt.TestDefinitionId,
                    t => t.Id,
                    (pt, t) => new { pt.ProgramId, t.Kind, t.ScoringMode }),
            cancellationToken).ConfigureAwait(false);

        var testRowsByProgramId = testRows.ToLookup(r => r.ProgramId);

        var items = pagePrograms
            .Select(p =>
            {
                var rows = testRowsByProgramId[p.Id];
                var hasPersonalityBattery = rows.Any(r => PersonalityBattery.Includes(r.Kind, r.ScoringMode));

                return ProgramMapping.ToListItemDto(p, rows.Count(), hasPersonalityBattery);
            })
            .ToList();

        return Result.Success(PagedResult<AdminProgramListItemDto>.Create(items, page, pageSize, totalCount));
    }

    private static IQueryable<AssessmentProgram> ApplySort(IQueryable<AssessmentProgram> query, string field, bool descending) => field switch
    {
        "name" => descending ? query.OrderByDescending(p => p.NameUz) : query.OrderBy(p => p.NameUz),
        "code" => descending ? query.OrderByDescending(p => p.Code) : query.OrderBy(p => p.Code),
        "createdAt" => descending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
        _ => descending ? query.OrderByDescending(p => p.DisplayOrder) : query.OrderBy(p => p.DisplayOrder),
    };
}
