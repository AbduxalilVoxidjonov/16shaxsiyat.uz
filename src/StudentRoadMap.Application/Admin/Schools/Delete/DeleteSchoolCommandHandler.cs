using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Schools.Delete;

/// <summary>
/// `docs/07` 3.1-bo'lim + `prompts/14` MAXSUS DIQQAT #5: soft delete (`School.MarkDeleted`);
/// maktabda (o'chirilmagan) o'quvchi bo'lsa `409 SCHOOL_HAS_STUDENTS` — jimgina ma'lumot
/// yo'qolib qolmasligi uchun (admin avval o'quvchilarni boshqa maktabga ko'chirishi/o'chirishi kerak).
/// </summary>
internal sealed class DeleteSchoolCommandHandler : IRequestHandler<DeleteSchoolCommand, Result>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public DeleteSchoolCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result> Handle(DeleteSchoolCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var school = await _executor.FirstOrDefaultAsync(
            _context.Schools.Where(s => s.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (school is null)
        {
            return Result.Failure(new Error(ProblemCodes.NotFound, "Maktab topilmadi."));
        }

        var hasStudents = await _executor.AnyAsync(
            _context.Students.Where(s => s.SchoolId == school.Id),
            cancellationToken).ConfigureAwait(false);

        if (hasStudents)
        {
            return Result.Failure(new Error(ProblemCodes.SchoolHasStudents, "Maktabda o'quvchilar bor — avval ularni o'chiring/ko'chiring."));
        }

        school.MarkDeleted(now);

        _context.Add(AuditLog.Create(
            AuditActions.SchoolDeleted,
            now,
            request.AdminUserId,
            entityType: "School",
            entityId: school.Id,
            afterJson: AuditSnapshot.Serialize(new { school.Id, DeletedAt = now }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
