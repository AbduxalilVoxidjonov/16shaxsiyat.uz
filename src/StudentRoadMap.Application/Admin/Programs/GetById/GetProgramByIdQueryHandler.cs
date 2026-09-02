using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.GetById;

/// <summary>Read-only — `AsNoTracking` (`docs/06` 4-bo'lim konvensiyasi).</summary>
internal sealed class GetProgramByIdQueryHandler : IRequestHandler<GetProgramByIdQuery, Result<AdminProgramDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public GetProgramByIdQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<AdminProgramDetailDto>> Handle(GetProgramByIdQuery request, CancellationToken cancellationToken)
    {
        var program = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.AssessmentPrograms).Where(p => p.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (program is null)
        {
            return Result.Failure<AdminProgramDetailDto>(new Error(ProblemCodes.NotFound, "Dastur topilmadi."));
        }

        var dto = await ProgramMapping.BuildDetailDtoAsync(_context, _executor, program, cancellationToken).ConfigureAwait(false);

        return Result.Success(dto);
    }
}
