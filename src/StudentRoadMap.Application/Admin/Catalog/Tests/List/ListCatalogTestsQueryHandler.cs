using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.List;

/// <summary>Read-only — `AsNoTracking`, saralash DB darajasida (`DisplayOrder`, keyin `Code`).</summary>
internal sealed class ListCatalogTestsQueryHandler : IRequestHandler<ListCatalogTestsQuery, Result<IReadOnlyList<CatalogTestListItemDto>>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public ListCatalogTestsQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<IReadOnlyList<CatalogTestListItemDto>>> Handle(ListCatalogTestsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AsNoTracking(_context.TestDefinitions);

        if (string.Equals(request.Kind, "System", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t => t.IsSystem);
        }
        else if (string.Equals(request.Kind, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t => !t.IsSystem);
        }

        if (request.IsSystem.HasValue)
        {
            query = query.Where(t => t.IsSystem == request.IsSystem.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<TestDefinitionStatus>(request.Status, ignoreCase: true, out var status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.ScoringMode) && Enum.TryParse<TestScoringMode>(request.ScoringMode, ignoreCase: true, out var scoringMode))
        {
            query = query.Where(t => t.ScoringMode == scoringMode);
        }

        var tests = await _executor.ToListAsync(
            query.OrderBy(t => t.DisplayOrder).ThenBy(t => t.Code),
            cancellationToken).ConfigureAwait(false);

        var testIds = tests.Select(t => t.Id).ToList();

        var questionCounts = await CatalogMapping.CountQuestionsAsync(_context, _executor, testIds, cancellationToken).ConfigureAwait(false);
        var scaleCounts = await CatalogMapping.CountScalesAsync(_context, _executor, testIds, cancellationToken).ConfigureAwait(false);
        var usedCounts = await CatalogMapping.CountUsedInProgramsAsync(_context, _executor, testIds, cancellationToken).ConfigureAwait(false);

        var items = tests
            .Select(t => CatalogMapping.ToListItemDto(
                t,
                questionCounts.GetValueOrDefault(t.Id),
                scaleCounts.GetValueOrDefault(t.Id),
                usedCounts.GetValueOrDefault(t.Id)))
            .ToList();

        return Result.Success<IReadOnlyList<CatalogTestListItemDto>>(items);
    }
}
