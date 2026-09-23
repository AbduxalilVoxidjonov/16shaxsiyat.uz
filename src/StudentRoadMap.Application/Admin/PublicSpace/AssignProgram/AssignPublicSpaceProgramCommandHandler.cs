using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.PublicSpace.AssignProgram;

/// <summary>
/// **Mexanizm QAYTA ISHLATILADI, NUSXA KO'CHIRILMAYDI:** biriktirishning o'zi
/// `ProgramSchoolAssignment.AssignAsync` da — `school_programs` yozuvi, idempotentlik va
/// `Program.Assigned` audit yozuvi maktab oqimi bilan AYNAN BIR XIL kod yo'lidan o'tadi.
/// Bu handler faqat ikkita ishni bajaradi:
/// <list type="number">
///   <item>ommaviy makonni topadi (admin `schoolId` yubormaydi — buyruq izohiga qarang);</item>
///   <item>javobni ommaviy bo'lim shartnomasiga (`AdminPublicSpaceDto`) o'giradi.</item>
/// </list>
/// MediatR orqali (`ISender.Send`) qayta ishlatish MUMKIN EMAS edi — sabab
/// `ProgramSchoolAssignment` izohida (ichma-ich tranzaksiya).
/// </summary>
internal sealed class AssignPublicSpaceProgramCommandHandler
    : IRequestHandler<AssignPublicSpaceProgramCommand, Result<AdminPublicSpaceDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly IAppSettings _appSettings;

    public AssignPublicSpaceProgramCommandHandler(
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

    public async Task<Result<AdminPublicSpaceDto>> Handle(AssignPublicSpaceProgramCommand request, CancellationToken cancellationToken)
    {
        var space = await PublicSpaceMapping.FindAsync(_context, _executor, cancellationToken).ConfigureAwait(false);

        if (space is null)
        {
            return Result.Failure<AdminPublicSpaceDto>(PublicSpaceMapping.NotConfigured());
        }

        var assignResult = await ProgramSchoolAssignment.AssignAsync(
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

        if (assignResult.IsFailure)
        {
            // Xato AYNAN o'sha ko'rinishda uzatiladi (masalan "Dastur topilmadi." → `404`).
            return Result.Failure<AdminPublicSpaceDto>(assignResult.Error);
        }

        var dto = await PublicSpaceMapping
            .BuildAsync(_context, _executor, _appSettings, space, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(dto);
    }
}
