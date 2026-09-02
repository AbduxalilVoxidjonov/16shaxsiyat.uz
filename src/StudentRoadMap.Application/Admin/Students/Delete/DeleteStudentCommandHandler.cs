using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Students.Delete;

/// <summary>
/// `docs/07` 3.2-bo'lim + `prompts/14` MAXSUS DIQQAT #5.
///
/// **`Hard = false`** — yumshoq o'chirish (`Student.MarkDeleted`), boshqa hech narsa o'chmaydi.
///
/// **`Hard = true`** — o'quvchi (yoki ota-ona) so'rovi bo'yicha TO'LIQ o'chirish: `AiAnalysis` →
/// `TestResult` → `Answer` → `AssessmentTest` → `Assessment` → `Student`, shu tartibda (bola
/// yozuvlar avval). `IgnoreQueryFilters` bilan avval SOFT o'chirilgan sessiyalar ham topiladi —
/// aks holda ular yetim qolib ketardi (`IAppDbContext.IgnoreQueryFilters` izohiga qarang).
///
/// **Audit'da faqat `{studentId, deletedAt, adminId, hard}`** — o'chirilgan shaxsiy ma'lumot
/// (ism, telefon) audit'ga HECH QACHON ko'chib qolmaydi (`prompts/14` MAXSUS DIQQAT #5, `CLAUDE.md` 6-band).
/// </summary>
internal sealed class DeleteStudentCommandHandler : IRequestHandler<DeleteStudentCommand, Result>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public DeleteStudentCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result> Handle(DeleteStudentCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var student = await _executor.FirstOrDefaultAsync(
            _context.IgnoreQueryFilters(_context.Students).Where(s => s.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (student is null)
        {
            return Result.Failure(new Error(ProblemCodes.NotFound, "O'quvchi topilmadi."));
        }

        if (request.Hard)
        {
            await HardDeleteAsync(student.Id, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            student.MarkDeleted(now);
        }

        _context.Add(AuditLog.Create(
            AuditActions.StudentDeleted,
            now,
            request.AdminUserId,
            entityType: "Student",
            entityId: student.Id,
            afterJson: AuditSnapshot.Serialize(new { StudentId = student.Id, DeletedAt = now, request.AdminUserId, request.Hard }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        if (request.Hard)
        {
            // `Student` o'zi ham shu SaveChanges bosqichida o'chiriladi (audit yozuvi undan
            // OLDIN qo'shildi, lekin `AuditLog.EntityId` faqat `Guid` — endi o'chirilayotgan
            // qatorga FK emas, shu sabab muammo yo'q).
            _context.Remove(student);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    private async Task HardDeleteAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var assessments = await _executor.ToListAsync(
            _context.IgnoreQueryFilters(_context.Assessments).Where(a => a.StudentId == studentId),
            cancellationToken).ConfigureAwait(false);

        if (assessments.Count == 0)
        {
            return;
        }

        var assessmentIds = assessments.Select(a => a.Id).ToList();

        var assessmentTests = await _executor.ToListAsync(
            _context.AssessmentTests.Where(t => assessmentIds.Contains(t.AssessmentId)),
            cancellationToken).ConfigureAwait(false);
        var assessmentTestIds = assessmentTests.Select(t => t.Id).ToList();

        var answers = await _executor.ToListAsync(
            _context.Answers.Where(a => assessmentTestIds.Contains(a.AssessmentTestId)),
            cancellationToken).ConfigureAwait(false);

        var testResults = await _executor.ToListAsync(
            _context.TestResults.Where(r => assessmentIds.Contains(r.AssessmentId)),
            cancellationToken).ConfigureAwait(false);

        var aiAnalyses = await _executor.ToListAsync(
            _context.AiAnalyses.Where(a => assessmentIds.Contains(a.AssessmentId)),
            cancellationToken).ConfigureAwait(false);

        foreach (var answer in answers)
        {
            _context.Remove(answer);
        }

        foreach (var testResult in testResults)
        {
            _context.Remove(testResult);
        }

        foreach (var aiAnalysis in aiAnalyses)
        {
            _context.Remove(aiAnalysis);
        }

        foreach (var assessmentTest in assessmentTests)
        {
            _context.Remove(assessmentTest);
        }

        foreach (var assessment in assessments)
        {
            _context.Remove(assessment);
        }
    }
}
