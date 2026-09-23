using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.PublicSpace.UnassignProgram;

/// <summary>
/// `AssignPublicSpaceProgramCommandHandler` bilan bir xil naqsh — mexanizm
/// `ProgramSchoolAssignment.UnassignAsync` da (idempotent, `Program.Unassigned` audit yozuvi),
/// bu handler faqat makonni topadi va javobni ommaviy shartnomaga o'giradi.
/// </summary>
internal sealed class UnassignPublicSpaceProgramCommandHandler
    : IRequestHandler<UnassignPublicSpaceProgramCommand, Result<AdminPublicSpaceDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly IAppSettings _appSettings;

    public UnassignPublicSpaceProgramCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        IIpHasher ipHasher,
        IAppSettings appSettings)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
        _appSettings = appSettings;
    }

    public async Task<Result<AdminPublicSpaceDto>> Handle(UnassignPublicSpaceProgramCommand request, CancellationToken cancellationToken)
    {
        var space = await PublicSpaceMapping.FindAsync(_context, _executor, cancellationToken).ConfigureAwait(false);

        if (space is null)
        {
            return Result.Failure<AdminPublicSpaceDto>(PublicSpaceMapping.NotConfigured());
        }

        var unassignResult = await ProgramSchoolAssignment.UnassignAsync(
            _context,
            _executor,
            _ipHasher,
            request.ProgramId,
            space.Id,
            request.AdminUserId,
            request.IpAddress,
            request.UserAgent,
            _dateTime.UtcNow,
            cancellationToken).ConfigureAwait(false);

        if (unassignResult.IsFailure)
        {
            return Result.Failure<AdminPublicSpaceDto>(unassignResult.Error);
        }

        var dto = await PublicSpaceMapping
            .BuildAsync(_context, _executor, _appSettings, space, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(dto);
    }
}
