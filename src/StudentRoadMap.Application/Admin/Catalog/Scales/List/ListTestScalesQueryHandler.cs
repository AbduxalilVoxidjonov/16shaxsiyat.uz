using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Scales.List;

internal sealed class ListTestScalesQueryHandler : IRequestHandler<ListTestScalesQuery, Result<IReadOnlyList<CatalogScaleItemDto>>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public ListTestScalesQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<IReadOnlyList<CatalogScaleItemDto>>> Handle(ListTestScalesQuery request, CancellationToken cancellationToken)
    {
        var testExists = await _executor.AnyAsync(
            _context.AsNoTracking(_context.TestDefinitions).Where(t => t.Id == request.TestDefinitionId),
            cancellationToken).ConfigureAwait(false);

        if (!testExists)
        {
            return Result.Failure<IReadOnlyList<CatalogScaleItemDto>>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        var scales = await _executor.ToListAsync(
            _context.AsNoTracking(_context.TestScales)
                .Where(s => s.TestDefinitionId == request.TestDefinitionId)
                .OrderBy(s => s.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var questionScaleCodes = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Questions)
                .Where(q => q.TestDefinitionId == request.TestDefinitionId)
                .Select(q => q.Scale),
            cancellationToken).ConfigureAwait(false);

        var countByScaleCode = questionScaleCodes.GroupBy(c => c, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        var items = scales
            .Select(s => CatalogMapping.ToScaleDto(s, countByScaleCode.GetValueOrDefault(s.Code)))
            .ToList();

        return Result.Success<IReadOnlyList<CatalogScaleItemDto>>(items);
    }
}
