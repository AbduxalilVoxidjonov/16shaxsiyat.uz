using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Programs.Update;

internal sealed class UpdateProgramCommandHandler : IRequestHandler<UpdateProgramCommand, Result<AdminProgramDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public UpdateProgramCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result<AdminProgramDetailDto>> Handle(UpdateProgramCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var program = await _executor.FirstOrDefaultAsync(
            _context.AssessmentPrograms.Where(p => p.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (program is null)
        {
            return Result.Failure<AdminProgramDetailDto>(new Error(ProblemCodes.NotFound, "Dastur topilmadi."));
        }

        var before = AuditSnapshot.Serialize(new { program.Id, program.NameUz, Visibility = program.Visibility.ToString(), program.DisplayOrder });

        var visibility = Enum.Parse<ProgramVisibility>(request.Visibility, ignoreCase: true);
        program.UpdateDetails(request.NameUz, request.DescriptionUz, request.DisplayOrder, now);
        if (program.Visibility != visibility)
        {
            program.SetVisibility(visibility, now);
        }

        var after = AuditSnapshot.Serialize(new { program.Id, program.NameUz, Visibility = program.Visibility.ToString(), program.DisplayOrder });

        _context.Add(AuditLog.Create(
            AuditActions.ProgramUpdated,
            now,
            request.AdminUserId,
            entityType: "AssessmentProgram",
            entityId: program.Id,
            beforeJson: before,
            afterJson: after,
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = await ProgramMapping.BuildDetailDtoAsync(_context, _executor, program, cancellationToken).ConfigureAwait(false);

        return Result.Success(dto);
    }
}
