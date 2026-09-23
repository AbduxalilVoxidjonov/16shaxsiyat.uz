using MediatR;
using StudentRoadMap.Application.Admin.Schools.LinkHealth;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Schools.ToggleActive;

/// <summary>`docs/07` 3.1-bo'lim: "Faol/nofaol". `IsActive = false` → ommaviy API `410 SCHOOL_INACTIVE` (`docs/04` 2.1).</summary>
internal sealed class ToggleSchoolActiveCommandHandler : IRequestHandler<ToggleSchoolActiveCommand, Result<AdminSchoolDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IAppSettings _appSettings;
    private readonly IIpHasher _ipHasher;
    private readonly IQrCodeGenerator _qrCodeGenerator;

    public ToggleSchoolActiveCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        IAppSettings appSettings,
        IIpHasher ipHasher,
        IQrCodeGenerator qrCodeGenerator)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _appSettings = appSettings;
        _ipHasher = ipHasher;
        _qrCodeGenerator = qrCodeGenerator;
    }

    public async Task<Result<AdminSchoolDetailDto>> Handle(ToggleSchoolActiveCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var school = await _executor.FirstOrDefaultAsync(
            _context.Schools.SchoolsOnly().Where(s => s.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (school is null)
        {
            return Result.Failure<AdminSchoolDetailDto>(new Error(ProblemCodes.NotFound, "Maktab topilmadi."));
        }

        var wasActive = school.IsActive;

        if (wasActive)
        {
            school.Deactivate(now);
        }
        else
        {
            school.Activate(now);
        }

        _context.Add(AuditLog.Create(
            AuditActions.SchoolToggledActive,
            now,
            request.AdminUserId,
            entityType: "School",
            entityId: school.Id,
            beforeJson: AuditSnapshot.Serialize(new { IsActive = wasActive }),
            afterJson: AuditSnapshot.Serialize(new { school.IsActive }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var stats = await SchoolMapping.ComputeStatsAsync(_context, _executor, school.Id, cancellationToken).ConfigureAwait(false);

        var linkHealth = await SchoolLinkHealthEvaluator.EvaluateOneAsync(_context, _executor, school.Id, cancellationToken).ConfigureAwait(false);


        var testIds = await SchoolTestAssignment.GetTestIdsAsync(_context, _executor, school.Id, cancellationToken).ConfigureAwait(false);

        return Result.Success(SchoolMapping.ToDetailDto(school, _appSettings, _qrCodeGenerator, stats, linkHealth, testIds));
    }
}
