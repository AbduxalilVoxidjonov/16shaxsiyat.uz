using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Assessments.Delete;

/// <summary>
/// `docs/07` 3.3-bo'lim: soft delete (`Assessment.MarkDeleted`) — `DeleteSchoolCommandHandler`
/// bilan bir xil uslub. Audit'da faqat `{assessmentId, deletedAt}` — shaxsiy ma'lumot yo'q
/// (`CLAUDE.md` 6-band).
/// </summary>
internal sealed class DeleteAssessmentCommandHandler : IRequestHandler<DeleteAssessmentCommand, Result>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public DeleteAssessmentCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result> Handle(DeleteAssessmentCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var assessment = await _executor.FirstOrDefaultAsync(
            _context.Assessments.Where(a => a.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (assessment is null)
        {
            return Result.Failure(new Error(ProblemCodes.NotFound, "Sessiya topilmadi."));
        }

        assessment.MarkDeleted(now);

        _context.Add(AuditLog.Create(
            AuditActions.AssessmentDeleted,
            now,
            request.AdminUserId,
            entityType: "Assessment",
            entityId: assessment.Id,
            afterJson: AuditSnapshot.Serialize(new { AssessmentId = assessment.Id, DeletedAt = now }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
