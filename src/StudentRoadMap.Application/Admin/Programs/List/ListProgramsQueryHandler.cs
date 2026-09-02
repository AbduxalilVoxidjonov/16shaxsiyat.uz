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

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<ProgramStatus>(request.Status, ignoreCase: true, out var status))
        {
            query = query.Where(p => p.Status == status);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == request.IsActive.Value);
        }

        var totalCount = await _executor.CountAsync(query, cancellationToken).ConfigureAwait(false);

        var (sortField, descending) = AdminSortSpec.Parse(request.Sort, defaultField: "displayOrder");
        var sortedQuery = ApplySort(query, sortField, descending);

        var pagePrograms = await _executor.ToListAsync(
            sortedQuery.Skip((page - 1) * pageSize).Take(pageSize),
            cancellationToken).ConfigureAwait(false);

        var programIds = pagePrograms.Select(p => p.Id).ToList();

        var testCountRows = await _executor.ToListAsync(
            _context.AsNoTracking(_context.ProgramTests)
                .Where(pt => programIds.Contains(pt.ProgramId))
                .Select(pt => pt.ProgramId),
            cancellationToken).ConfigureAwait(false);

        var testCountByProgramId = testCountRows
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        var items = pagePrograms
            .Select(p => ProgramMapping.ToListItemDto(p, testCountByProgramId.GetValueOrDefault(p.Id)))
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
