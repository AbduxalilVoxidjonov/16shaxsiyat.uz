using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Application.Admin.Schools.RegenerateEntryCode;

/// <summary>
/// `RegenerateSchoolLinkCommandHandler` bilan bir xil naqsh. Audit'da kod QIYMATI SAQLANMAYDI
/// (u — havola tokeni kabi tarqatiladigan sir) — faqat "qachon qayta yaratilgani".
/// Unikallik: `SchoolEntryCodeAllocator` DB'dan tekshirib bo'sh kod tanlaydi; ChIN poyga
/// holati `ux_schools_entry_code` bilan `409 UNIQUE_CONSTRAINT_CONFLICT` ga tushadi.
/// </summary>
internal sealed class RegenerateSchoolEntryCodeCommandHandler
    : IRequestHandler<RegenerateSchoolEntryCodeCommand, Result<RegenerateSchoolEntryCodeResult>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IEntryCodeGenerator _entryCodeGenerator;
    private readonly IIpHasher _ipHasher;

    public RegenerateSchoolEntryCodeCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        IEntryCodeGenerator entryCodeGenerator,
        IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _entryCodeGenerator = entryCodeGenerator;
        _ipHasher = ipHasher;
    }

    public async Task<Result<RegenerateSchoolEntryCodeResult>> Handle(
        RegenerateSchoolEntryCodeCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var school = await _executor.FirstOrDefaultAsync(
            _context.Schools.SchoolsOnly().Where(s => s.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (school is null)
        {
            return Result.Failure<RegenerateSchoolEntryCodeResult>(new Error(ProblemCodes.NotFound, "Maktab topilmadi."));
        }

        var newEntryCode = await SchoolEntryCodeAllocator
            .AllocateUniqueAsync(_context, _executor, _entryCodeGenerator, cancellationToken)
            .ConfigureAwait(false);

        school.RegenerateEntryCode(newEntryCode, now);

        _context.Add(AuditLog.Create(
            SchoolEntryCodeAuditActions.Regenerated,
            now,
            request.AdminUserId,
            entityType: "School",
            entityId: school.Id,
            afterJson: AuditSnapshot.Serialize(new { school.Id, RegeneratedAt = now }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(new RegenerateSchoolEntryCodeResult(SchoolEntryCode.Format(newEntryCode)));
    }
}
