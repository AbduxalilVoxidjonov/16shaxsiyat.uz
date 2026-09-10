using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Catalog;
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

        var questionIds = questions.Select(q => q.Id).ToList();

        // `docs/18` §5 — variantlar alohida batch so'rov bilan (`GetCatalogTestPreviewQueryHandler`
        // naqshi): `AsNoTracking` ro'yxat so'rovida navigatsiya kolleksiyasi bo'sh bo'ladi.
        var options = await _executor.ToListAsync(
            _context.AsNoTracking(_context.AnswerOptions).Where(o => questionIds.Contains(o.QuestionId)),
            cancellationToken).ConfigureAwait(false);

        var optionsByQuestion = options.GroupBy(o => o.QuestionId).ToDictionary(g => g.Key, g => (IReadOnlyList<AnswerOption>)g.ToList());

        var items = questions
            .Select(q => CatalogMapping.ToQuestionDto(q, scaleNames, optionsByQuestion.GetValueOrDefault(q.Id, [])))
            .ToList();

        return Result.Success<IReadOnlyList<CatalogQuestionItemDto>>(items);
    }
}
