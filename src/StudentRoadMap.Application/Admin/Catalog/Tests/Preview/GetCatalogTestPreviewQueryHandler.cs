using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Preview;

/// <summary>Read-only, `AsNoTracking`. Savol darajasida `scale`/`scaleDirection` YO'Q (`CLAUDE.md` 9-qoida ruhi — bu ADMIN endpoint, lekin "o'quvchi ko'radigan ko'rinish" nomi bilan mos: shkala kodi emas, faqat matn/variantlar).</summary>
internal sealed class GetCatalogTestPreviewQueryHandler : IRequestHandler<GetCatalogTestPreviewQuery, Result<CatalogTestPreviewDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public GetCatalogTestPreviewQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<CatalogTestPreviewDto>> Handle(GetCatalogTestPreviewQuery request, CancellationToken cancellationToken)
    {
        var test = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.TestDefinitions).Where(t => t.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (test is null)
        {
            return Result.Failure<CatalogTestPreviewDto>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        var questions = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Questions)
                .Where(q => q.TestDefinitionId == test.Id && q.IsActive)
                .OrderBy(q => q.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var questionIds = questions.Select(q => q.Id).ToList();

        var options = await _executor.ToListAsync(
            _context.AsNoTracking(_context.AnswerOptions)
                .Where(o => questionIds.Contains(o.QuestionId))
                .OrderBy(o => o.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var optionsByQuestion = options.GroupBy(o => o.QuestionId).ToDictionary(g => g.Key, g => g.ToList());

        var questionDtos = questions
            .Select(q => new CatalogPreviewQuestionDto(
                q.Id,
                q.TextUz,
                q.QuestionType.ToString(),
                (optionsByQuestion.TryGetValue(q.Id, out var qOptions) ? qOptions : [])
                    .Select(o => new CatalogPreviewOptionDto(o.Id, o.TextUz, o.Value))
                    .ToList()))
            .ToList();

        var scales = await _executor.ToListAsync(
            _context.AsNoTracking(_context.TestScales)
                .Where(s => s.TestDefinitionId == test.Id)
                .OrderBy(s => s.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var scaleDtos = scales
            .Select(s => new CatalogPreviewScaleDto(s.Code, s.NameUz, s.DescriptionUz))
            .ToList();

        var dto = new CatalogTestPreviewDto(
            test.Id,
            test.Code,
            test.NameUz,
            test.DescriptionUz,
            test.EstimatedMinutes,
            test.PageSize,
            test.ShuffleQuestions,
            questionDtos,
            scaleDtos);

        return Result.Success(dto);
    }
}
