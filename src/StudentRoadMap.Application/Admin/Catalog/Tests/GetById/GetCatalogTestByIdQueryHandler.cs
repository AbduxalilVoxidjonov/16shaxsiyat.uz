using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.GetById;

internal sealed class GetCatalogTestByIdQueryHandler : IRequestHandler<GetCatalogTestByIdQuery, Result<CatalogTestDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public GetCatalogTestByIdQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<CatalogTestDetailDto>> Handle(GetCatalogTestByIdQuery request, CancellationToken cancellationToken)
    {
        var test = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.TestDefinitions).Where(t => t.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (test is null)
        {
            return Result.Failure<CatalogTestDetailDto>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        var questionCount = await _executor.CountAsync(
            _context.AsNoTracking(_context.Questions).Where(q => q.TestDefinitionId == test.Id),
            cancellationToken).ConfigureAwait(false);

        var scaleCount = await _executor.CountAsync(
            _context.AsNoTracking(_context.TestScales).Where(s => s.TestDefinitionId == test.Id),
            cancellationToken).ConfigureAwait(false);

        var usedInProgramCount = await _executor.CountAsync(
            _context.AsNoTracking(_context.ProgramTests).Where(pt => pt.TestDefinitionId == test.Id),
            cancellationToken).ConfigureAwait(false);

        return Result.Success(CatalogMapping.ToDetailDto(test, questionCount, scaleCount, usedInProgramCount));
    }
}
