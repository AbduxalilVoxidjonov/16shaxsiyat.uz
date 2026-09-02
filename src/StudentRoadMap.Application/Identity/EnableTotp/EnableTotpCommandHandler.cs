using System.Security.Cryptography;
using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Identity.EnableTotp;

internal sealed class EnableTotpCommandHandler : IRequestHandler<EnableTotpCommand, Result<EnableTotpResult>>
{
    private const int BackupCodeCount = 8;
    private const string IssuerName = "Salohiyat";

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IEncryptionService _encryptionService;
    private readonly ITotpService _totpService;
    private readonly IPasswordHasher _passwordHasher;

    public EnableTotpCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        IEncryptionService encryptionService,
        ITotpService totpService,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _encryptionService = encryptionService;
        _totpService = totpService;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<EnableTotpResult>> Handle(EnableTotpCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var user = await _executor.FirstOrDefaultAsync(
            _context.AdminUsers.Where(u => u.Id == request.AdminUserId),
            cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure<EnableTotpResult>(new Error(ProblemCodes.NotFound, "Foydalanuvchi topilmadi."));
        }

        if (user.TotpEnabled)
        {
            return Result.Failure<EnableTotpResult>(new Error(ProblemCodes.TotpAlreadyEnabled, "TOTP allaqachon yoqilgan."));
        }

        var secret = _totpService.GenerateSecret();
        user.EnableTotp(_encryptionService.Encrypt(secret), now);

        var backupCodes = new List<string>(BackupCodeCount);
        for (var i = 0; i < BackupCodeCount; i++)
        {
            var code = GenerateBackupCode();
            backupCodes.Add(code);
            _context.Add(AdminTotpBackupCode.Create(Guid.NewGuid(), user.Id, _passwordHasher.Hash(code), now));
        }

        _context.Add(AuditLog.Create(AuditActions.AuthTotpEnabled, now, user.Id));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var otpauthUri = _totpService.BuildOtpauthUri(secret, user.Username, IssuerName);

        return Result.Success(new EnableTotpResult(secret, otpauthUri, backupCodes));
    }

    /// <summary>8 xonali raqamli zaxira kod — kiritish oson, `RandomNumberGenerator` bilan kriptografik tasodifiy.</summary>
    private static string GenerateBackupCode() =>
        RandomNumberGenerator.GetInt32(0, 100_000_000).ToString("D8", System.Globalization.CultureInfo.InvariantCulture);
}
