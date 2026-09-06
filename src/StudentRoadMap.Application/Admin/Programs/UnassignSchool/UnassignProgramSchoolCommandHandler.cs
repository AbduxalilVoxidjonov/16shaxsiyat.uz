using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.UnassignSchool;

/// <summary>
/// `prompts/34` E15-band. Mexanizm `ProgramSchoolAssignment.UnassignAsync` da —
/// `AssignProgramSchoolCommandHandler` bilan bir xil sabab (izohiga qarang).
/// Idempotent — biriktirilmagan bo'lsa hech narsa qilinmaydi (409 emas).
/// </summary>
internal sealed class UnassignProgramSchoolCommandHandler : IRequestHandler<UnassignProgramSchoolCommand, Result<AdminProgramDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public UnassignProgramSchoolCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result<AdminProgramDetailDto>> Handle(UnassignProgramSchoolCommand request, CancellationToken cancellationToken)
    {
        var result = await ProgramSchoolAssignment.UnassignAsync(
            _context,
            _executor,
            _ipHasher,
            request.ProgramId,
            request.SchoolId,
            request.AdminUserId,
            request.IpAddress,
            request.UserAgent,
            _dateTime.UtcNow,
            cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return Result.Failure<AdminProgramDetailDto>(result.Error);
        }

        var dto = await ProgramMapping.BuildDetailDtoAsync(_context, _executor, result.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(dto);
    }
}
