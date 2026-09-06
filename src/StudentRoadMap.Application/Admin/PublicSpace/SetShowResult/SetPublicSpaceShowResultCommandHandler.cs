using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.PublicSpace.SetShowResult;

/// <summary>
/// `School.SetShowResultToStudent` domen metodini chaqiradi (qayta yozilmaydi) va audit
/// yozuvini qoldiradi. Qiymat o'zgarmagan bo'lsa ham `SaveChanges` chaqiriladi — audit
/// jurnalida "admin buni ataylab tasdiqladi" fakti qolishi kerak (bayroq maxfiylik bilan
/// bog'liq: natija foydalanuvchiga ko'rinishi).
/// </summary>
internal sealed class SetPublicSpaceShowResultCommandHandler
    : IRequestHandler<SetPublicSpaceShowResultCommand, Result<AdminPublicSpaceDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IAppSettings _appSettings;
    private readonly IIpHasher _ipHasher;

    public SetPublicSpaceShowResultCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        IAppSettings appSettings,
        IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _appSettings = appSettings;
        _ipHasher = ipHasher;
    }

    public async Task<Result<AdminPublicSpaceDto>> Handle(SetPublicSpaceShowResultCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var space = await PublicSpaceMapping.FindAsync(_context, _executor, cancellationToken).ConfigureAwait(false);

        if (space is null)
        {
            return Result.Failure<AdminPublicSpaceDto>(PublicSpaceMapping.NotConfigured());
        }

        var before = space.ShowResultToStudent;

        space.SetShowResultToStudent(request.Enabled, now);

        _context.Add(AuditLog.Create(
            PublicSpaceAuditActions.ShowResultChanged,
            now,
            request.AdminUserId,
            entityType: "School",
            entityId: space.Id,
            beforeJson: AuditSnapshot.Serialize(new { ShowResultToStudent = before }),
            afterJson: AuditSnapshot.Serialize(new { space.ShowResultToStudent }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = await PublicSpaceMapping
            .BuildAsync(_context, _executor, _appSettings, space, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(dto);
    }
}
