using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Programs.AssignSchool;

/// <summary>`prompts/34` E15-band. Idempotent — allaqachon biriktirilgan bo'lsa hech narsa qilinmaydi (409 emas).</summary>
internal sealed class AssignProgramSchoolCommandHandler : IRequestHandler<AssignProgramSchoolCommand, Result<AdminProgramDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public AssignProgramSchoolCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result<AdminProgramDetailDto>> Handle(AssignProgramSchoolCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var program = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.AssessmentPrograms).Where(p => p.Id == request.ProgramId),
            cancellationToken).ConfigureAwait(false);

        if (program is null)
        {
            return Result.Failure<AdminProgramDetailDto>(new Error(ProblemCodes.NotFound, "Dastur topilmadi."));
        }

        var schoolExists = await _executor.AnyAsync(
            _context.Schools.Where(s => s.Id == request.SchoolId),
            cancellationToken).ConfigureAwait(false);

        if (!schoolExists)
        {
            return Result.Failure<AdminProgramDetailDto>(new Error(ProblemCodes.NotFound, "Maktab topilmadi."));
        }

        var alreadyAssigned = await _executor.AnyAsync(
            _context.SchoolPrograms.Where(sp => sp.ProgramId == request.ProgramId && sp.SchoolId == request.SchoolId),
            cancellationToken).ConfigureAwait(false);

        if (!alreadyAssigned)
        {
            _context.Add(SchoolProgram.Create(Guid.NewGuid(), request.SchoolId, request.ProgramId, now));

            _context.Add(AuditLog.Create(
                AuditActions.ProgramSchoolAssigned,
                now,
                request.AdminUserId,
                entityType: "AssessmentProgram",
                entityId: program.Id,
                afterJson: AuditSnapshot.Serialize(new { program.Id, request.SchoolId }),
                ipHash: _ipHasher.Hash(request.IpAddress),
                userAgent: request.UserAgent));

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var dto = await ProgramMapping.BuildDetailDtoAsync(_context, _executor, program, cancellationToken).ConfigureAwait(false);

        return Result.Success(dto);
    }
}
