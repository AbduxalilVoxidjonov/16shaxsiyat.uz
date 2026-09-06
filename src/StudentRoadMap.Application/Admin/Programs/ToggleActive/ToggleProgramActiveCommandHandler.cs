using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Programs.ToggleActive;

/// <summary>
/// `POST /api/admin/programs/{id}/toggle-active` — `Active ⇄ Paused` (faqat `Published`
/// doirasida). `Draft`/`Archived` dasturda domen `PROGRAM_INVALID_TRANSITION` tashlaydi.
/// </summary>
internal sealed class ToggleProgramActiveCommandHandler : IRequestHandler<ToggleProgramActiveCommand, Result<AdminProgramDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public ToggleProgramActiveCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result<AdminProgramDetailDto>> Handle(ToggleProgramActiveCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var program = await _executor.FirstOrDefaultAsync(
            _context.AssessmentPrograms.Where(p => p.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (program is null)
        {
            return Result.Failure<AdminProgramDetailDto>(new Error(ProblemCodes.NotFound, "Dastur topilmadi."));
        }

        // Domen qo'riqchisi (`Activate`/`Deactivate`): faqat `Published` dasturda ishlaydi —
        // qoralama yoki arxivlangan dasturda `409 PROGRAM_INVALID_TRANSITION`. UI bunday
        // tugmani umuman ko'rsatmaydi, lekin API to'g'ridan-to'g'ri chaqirilsa ham
        // ziddiyatli qator (masalan "Arxiv + Faol") hosil bo'lmasligi kerak.
        var stateBefore = program.State;

        if (program.IsActive)
        {
            program.Deactivate(now);
        }
        else
        {
            program.Activate(now);
        }

        _context.Add(AuditLog.Create(
            AuditActions.ProgramToggledActive,
            now,
            request.AdminUserId,
            entityType: "AssessmentProgram",
            entityId: program.Id,
            beforeJson: AuditSnapshot.Serialize(new { State = stateBefore.ToString() }),
            afterJson: AuditSnapshot.Serialize(new { State = program.State.ToString() }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = await ProgramMapping.BuildDetailDtoAsync(_context, _executor, program, cancellationToken).ConfigureAwait(false);

        return Result.Success(dto);
    }
}
