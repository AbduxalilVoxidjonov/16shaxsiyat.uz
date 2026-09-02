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
        var testExists = await _executor.AnyAsync(
            _context.AsNoTracking(_context.TestDefinitions).Where(t => t.Id == request.TestDefinitionId),
            cancellationToken).ConfigureAwait(false);

        if (!testExists)
        {
            return Result.Failure<IReadOnlyList<CatalogQuestionItemDto>>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        var questions = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Questions)
                .Where(q => q.TestDefinitionId == request.TestDefinitionId)
                .OrderBy(q => q.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var items = questions.Select(CatalogMapping.ToQuestionDto).ToList();

        return Result.Success<IReadOnlyList<CatalogQuestionItemDto>>(items);
    }
}
