using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Questions.List;

internal sealed class ListTestQuestionsQueryHandler : IRequestHandler<ListTestQuestionsQuery, Result<IReadOnlyList<CatalogQuestionItemDto>>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public ListTestQuestionsQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<IReadOnlyList<CatalogQuestionItemDto>>> Handle(ListTestQuestionsQuery request, CancellationToken cancellationToken)
    {
        // Anketaning O'ZI kerak (`AnyAsync` emas): `ScaleNameUz`ni aniqlash uchun
        // `ScoringStrategyCode` lozim (`SystemScaleCatalog` kaliti).
        var test = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.TestDefinitions).Where(t => t.Id == request.TestDefinitionId),
            cancellationToken).ConfigureAwait(false);

        if (test is null)
        {
            return Result.Failure<IReadOnlyList<CatalogQuestionItemDto>>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        var scaleNames = await CatalogMapping.LoadScaleNamesAsync(_context, _executor, test, cancellationToken).ConfigureAwait(false);

        var questions = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Questions)
                .Where(q => q.TestDefinitionId == request.TestDefinitionId)
                .OrderBy(q => q.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var items = questions.Select(q => CatalogMapping.ToQuestionDto(q, scaleNames)).ToList();

        return Result.Success<IReadOnlyList<CatalogQuestionItemDto>>(items);
    }
}
