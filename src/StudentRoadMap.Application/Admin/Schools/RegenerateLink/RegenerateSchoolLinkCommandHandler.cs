using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Schools.RegenerateLink;

/// <summary>
/// `docs/07` 3.1-bo'lim + `prompts/14` MAXSUS DIQQAT #4. Audit'da token QIYMATI SAQLANMAYDI
/// (u — havola siri, parol bilan bir xil darajada maxfiy) — faqat "qachon regenerate qilingani".
/// </summary>
internal sealed class RegenerateSchoolLinkCommandHandler : IRequestHandler<RegenerateSchoolLinkCommand, Result<RegenerateSchoolLinkResult>>
{
    private const int AccessTokenByteLength = 32;

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IQrCodeGenerator _qrCodeGenerator;
    private readonly IAppSettings _appSettings;
    private readonly IIpHasher _ipHasher;

    public RegenerateSchoolLinkCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        ITokenGenerator tokenGenerator,
        IQrCodeGenerator qrCodeGenerator,
        IAppSettings appSettings,
        IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _tokenGenerator = tokenGenerator;
        _qrCodeGenerator = qrCodeGenerator;
        _appSettings = appSettings;
        _ipHasher = ipHasher;
    }

    public async Task<Result<RegenerateSchoolLinkResult>> Handle(RegenerateSchoolLinkCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var school = await _executor.FirstOrDefaultAsync(
            _context.Schools.Where(s => s.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (school is null)
        {
            return Result.Failure<RegenerateSchoolLinkResult>(new Error(ProblemCodes.NotFound, "Maktab topilmadi."));
        }

        var newAccessToken = _tokenGenerator.GenerateUrlSafeToken(AccessTokenByteLength);
        school.RegenerateAccessToken(newAccessToken, now);

        _context.Add(AuditLog.Create(
            AuditActions.SchoolLinkRegenerated,
            now,
            request.AdminUserId,
            entityType: "School",
            entityId: school.Id,
            afterJson: AuditSnapshot.Serialize(new { school.Id, RegeneratedAt = now }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var publicUrl = SchoolMapping.BuildPublicUrl(school, _appSettings);
        var qrCodeBase64 = _qrCodeGenerator.GeneratePngBase64(publicUrl);

        return Result.Success(new RegenerateSchoolLinkResult(publicUrl, qrCodeBase64));
    }
}
