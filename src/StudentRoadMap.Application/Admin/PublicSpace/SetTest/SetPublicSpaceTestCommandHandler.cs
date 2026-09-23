using MediatR;
using StudentRoadMap.Application.Admin.Catalog.Tests.Assignment;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.PublicSpace.SetTest;

/// <summary>
/// Ommaviy makonga testni biriktirish/olib tashlash. Xatolar: test yo'q — `404`; YANGI
/// biriktirishda test arxivlangan — `409 TEST_ARCHIVED`; makon sozlanmagan — `409
/// PUBLIC_SPACE_NOT_CONFIGURED`. Olib tashlashda test dasturi bo'lmasa — hech narsa
/// qilinmaydi (idempotent), test dasturi YARATILMAYDI.
/// </summary>
internal sealed class SetPublicSpaceTestCommandHandler : IRequestHandler<SetPublicSpaceTestCommand, Result<AdminPublicSpaceDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly IAppSettings _appSettings;

    public SetPublicSpaceTestCommandHandler(
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

    public async Task<Result<AdminPublicSpaceDto>> Handle(SetPublicSpaceTestCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var space = await PublicSpaceMapping.FindAsync(_context, _executor, cancellationToken).ConfigureAwait(false);
        if (space is null)
        {
            return Result.Failure<AdminPublicSpaceDto>(PublicSpaceMapping.NotConfigured());
        }

        var test = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.TestDefinitions).Where(t => t.Id == request.TestDefinitionId),
            cancellationToken).ConfigureAwait(false);

        if (test is null)
        {
            return Result.Failure<AdminPublicSpaceDto>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        AssessmentProgram? program;
        if (request.Linked)
        {
            var existing = await TestPrograms.FindAsync(_context, _executor, test.Id, cancellationToken).ConfigureAwait(false);
            if (test.Status == TestDefinitionStatus.Archived && existing is null)
            {
                return Result.Failure<AdminPublicSpaceDto>(TestAssignmentErrors.Archived());
            }

            (program, _) = await TestPrograms.EnsureAsync(_context, _executor, test, request.AdminUserId, now, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            program = await TestPrograms.FindAsync(_context, _executor, test.Id, cancellationToken).ConfigureAwait(false);
        }

        if (program is not null)
        {
            var changed = await TestPrograms
                .SetPublicSpaceLinkAsync(_context, _executor, program.Id, space.Id, request.Linked, now, cancellationToken)
                .ConfigureAwait(false);

            if (changed)
            {
                _context.Add(AuditLog.Create(
                    AuditActions.CatalogTestAssignmentUpdated,
                    now,
                    request.AdminUserId,
                    entityType: "TestDefinition",
                    entityId: test.Id,
                    afterJson: AuditSnapshot.Serialize(new { IsInPublicSpace = request.Linked }),
                    ipHash: _ipHasher.Hash(request.IpAddress),
                    userAgent: request.UserAgent));
            }

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var dto = await PublicSpaceMapping.BuildAsync(_context, _executor, _appSettings, space, cancellationToken).ConfigureAwait(false);

        return Result.Success(dto);
    }
}
