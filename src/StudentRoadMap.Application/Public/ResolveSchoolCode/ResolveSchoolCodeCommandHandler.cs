using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Admin.Schools;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Application.Public.ResolveSchoolCode;

/// <summary>
/// `docs/08` 3a-bo'lim. Kod normalizatsiya qilinadi (`SchoolEntryCode.Normalize`), so'ng
/// `Kind == School && IsActive` maktab qidiriladi (`IsDeleted` — global query filtr).
///
/// **BITTA umumiy xato** — `404 SCHOOL_CODE_INVALID` — quyidagi HAMMA holat uchun: format
/// noto'g'ri, bunday kod yo'q, maktab nofaol, o'chirilgan, ommaviy makon (`EntryCode == null`,
/// so'rovga umuman tushmaydi). Sabab: `GET /schools/{slug}` da `410 SCHOOL_INACTIVE` bor,
/// chunki u yerda slug allaqachon ma'lum (havola ochiq tarqatilgan). Bu yerda esa kod —
/// YAGONA sir; "nofaol" ≠ "yo'q" farqi kodni sanab chiqayotganga (enumeration) tasdiq
/// bergan bo'lardi.
///
/// **Audit:** faqat muvaffaqiyatsiz urinish (`SchoolCode.ResolveFailed`, IP xeshi + UA, kod
/// qiymati YO'Q). Muvaffaqiyat audit qilinmaydi — u havola ochilishi bilan bir xil ma'noda
/// va keyingi qadamda `school_link_views` hisoblagichiga tushadi.
/// </summary>
internal sealed class ResolveSchoolCodeCommandHandler : IRequestHandler<ResolveSchoolCodeCommand, Result<ResolveSchoolCodeResult>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public ResolveSchoolCodeCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result<ResolveSchoolCodeResult>> Handle(ResolveSchoolCodeCommand request, CancellationToken cancellationToken)
    {
        var normalized = SchoolEntryCode.Normalize(request.Code);

        School? school = null;
        if (normalized is not null)
        {
            school = await _executor.FirstOrDefaultAsync(
                _context.AsNoTracking(_context.Schools)
                    .Where(s => s.Kind == SchoolKind.School && s.IsActive && s.EntryCode == normalized),
                cancellationToken).ConfigureAwait(false);
        }

        if (school is null)
        {
            return await RejectAsync(request, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(new ResolveSchoolCodeResult(school.Slug.Value, school.AccessToken));
    }

    private async Task<Result<ResolveSchoolCodeResult>> RejectAsync(ResolveSchoolCodeCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        _context.Add(AuditLog.Create(
            SchoolEntryCodeAuditActions.ResolveFailed,
            now,
            adminUserId: null,
            entityType: "School",
            entityId: null,
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Failure<ResolveSchoolCodeResult>(new Error(
            ProblemCodes.SchoolCodeInvalid,
            "Kod topilmadi. Maktabingizdan tekshiring."));
    }
}
