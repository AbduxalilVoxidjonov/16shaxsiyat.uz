using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Assignment;

/// <summary>Test dasturi bo'lmasa — standart bo'sh qiymatlar (`IsConfigured = false`), YARATILMAYDI.</summary>
internal sealed class GetTestAssignmentQueryHandler : IRequestHandler<GetTestAssignmentQuery, Result<AdminTestAssignmentDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public GetTestAssignmentQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<AdminTestAssignmentDto>> Handle(GetTestAssignmentQuery request, CancellationToken cancellationToken)
    {
        var test = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.TestDefinitions).Where(t => t.Id == request.TestDefinitionId),
            cancellationToken).ConfigureAwait(false);

        if (test is null)
        {
            return Result.Failure<AdminTestAssignmentDto>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        var program = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.AssessmentPrograms).Where(p => p.OwnerTestDefinitionId == test.Id),
            cancellationToken).ConfigureAwait(false);

        var dto = await TestAssignmentMapping.BuildAsync(_context, _executor, test, program, cancellationToken).ConfigureAwait(false);

        return Result.Success(dto);
    }
}
