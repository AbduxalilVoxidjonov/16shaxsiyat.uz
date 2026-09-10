using MediatR;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Sections.List;

/// <summary>Read-only, `AsNoTracking` — tizim testida ham ochiq (ro'yxat har doim bo'sh, B-3).</summary>
internal sealed class ListTestSectionsQueryHandler : IRequestHandler<ListTestSectionsQuery, Result<IReadOnlyList<CatalogSectionItemDto>>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public ListTestSectionsQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<IReadOnlyList<CatalogSectionItemDto>>> Handle(ListTestSectionsQuery request, CancellationToken cancellationToken)
    {
        var testExists = await _executor.AnyAsync(
            _context.AsNoTracking(_context.TestDefinitions).Where(t => t.Id == request.TestDefinitionId),
            cancellationToken).ConfigureAwait(false);

        if (!testExists)
        {
            return Result.Failure<IReadOnlyList<CatalogSectionItemDto>>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        var sections = await _executor.ToListAsync(
            _context.AsNoTracking(_context.QuestionSections)
                .Where(s => s.TestDefinitionId == request.TestDefinitionId)
                .OrderBy(s => s.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var items = sections.Select(CatalogMapping.ToSectionDto).ToList();

        return Result.Success<IReadOnlyList<CatalogSectionItemDto>>(items);
    }
}
