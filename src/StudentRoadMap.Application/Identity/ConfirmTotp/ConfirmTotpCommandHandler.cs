using System.Globalization;
using System.Security.Cryptography;
using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Identity.ConfirmTotp;

/// <summary>
/// O'rnatishning 2-bosqichi: kodni tekshiradi, 2FA'ni YOQADI va zaxira kodlarni beradi.
///
/// <para>
/// **Zaxira kodlar aynan shu yerda yoziladi** (ilgari `enable` da edi). Sabab: tasdiqlanmagan
/// o'rnatish uchun DB'da 8 ta xeshlangan kod qolib ketmasligi kerak — ular hech qachon
/// yoqilmaydigan 2FA uchun keraksiz maxfiy material bo'lardi, takroriy `enable` chaqiruvlarida
/// esa eskirgan kodlar to'planib qolardi. Bu yerda avval SHU foydalanuvchining barcha eski
/// (agar `disable` dan keyin qolib ketgan bo'lsa) kodlari o'chiriladi, keyin yangi 8 tasi
/// yoziladi — "yoqilgan 2FA = aynan bitta amaldagi to'plam" invarianti saqlanadi.
/// </para>
///
/// <para>
/// Tasdiqlashda ishlatilgan vaqt qadami `RegisterTotpStepUsed` bilan qayd etiladi: xuddi shu
/// kod bilan darhol login qilib bo'lmaydi (qayta ishlatishga qarshi himoya,
/// `docs/13-auth-va-jwt.md` MAXSUS DIQQAT 5-band).
/// </para>
/// </summary>
internal sealed class ConfirmTotpCommandHandler : IRequestHandler<ConfirmTotpCommand, Result<ConfirmTotpResult>>
{
    private const int BackupCodeCount = 8;

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IEncryptionService _encryptionService;
    private readonly ITotpService _totpService;
    private readonly IPasswordHasher _passwordHasher;

    public ConfirmTotpCommandHandler(
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

    public async Task<Result<ConfirmTotpResult>> Handle(ConfirmTotpCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var user = await _executor.FirstOrDefaultAsync(
            _context.AdminUsers.Where(u => u.Id == request.AdminUserId),
            cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure<ConfirmTotpResult>(new Error(ProblemCodes.NotFound, "Foydalanuvchi topilmadi."));
        }

        if (user.TotpEnabled)
        {
            return Result.Failure<ConfirmTotpResult>(new Error(ProblemCodes.TotpAlreadyEnabled, "TOTP allaqachon yoqilgan."));
        }

        if (user.PendingTotpSecretEncrypted is null)
        {
            return Result.Failure<ConfirmTotpResult>(new Error(
                ProblemCodes.TotpEnrollmentNotStarted,
                "2FA o'rnatish boshlanmagan. Avval \"Yoqish\" tugmasini bosing."));
        }

        if (!user.HasValidPendingTotpEnrollment(now))
        {
            // Eskirgan sirni darhol tozalaymiz — keyingi urinish toza boshlansin.
            user.CancelTotpEnrollment(now);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result.Failure<ConfirmTotpResult>(new Error(
                ProblemCodes.TotpEnrollmentExpired,
                "2FA o'rnatish muddati tugadi. QR kodni qaytadan oling."));
        }

        var secret = _encryptionService.Decrypt(user.PendingTotpSecretEncrypted);

        // Kutish holatidagi sir uchun `lastUsedStep` yo'q — u hali hech qachon ishlatilmagan.
        if (!_totpService.TryValidate(secret, request.Code, now, lastUsedStep: null, out var matchedStep))
        {
            return Result.Failure<ConfirmTotpResult>(new Error(
                ProblemCodes.TotpCodeInvalid,
                "Tasdiqlash kodi noto'g'ri. Ilovadagi joriy kodni kiriting."));
        }

        user.ConfirmTotpEnrollment(now);
        user.RegisterTotpStepUsed(matchedStep, now);

        var staleCodes = await _executor.ToListAsync(
            _context.AdminTotpBackupCodes.Where(c => c.AdminUserId == user.Id),
            cancellationToken).ConfigureAwait(false);

        foreach (var staleCode in staleCodes)
        {
            _context.Remove(staleCode);
        }

        var backupCodes = new List<string>(BackupCodeCount);
        for (var i = 0; i < BackupCodeCount; i++)
        {
            var code = GenerateBackupCode();
            backupCodes.Add(code);
            _context.Add(AdminTotpBackupCode.Create(Guid.NewGuid(), user.Id, _passwordHasher.Hash(code), now));
        }

        _context.Add(AuditLog.Create(AuditActions.AuthTotpEnabled, now, user.Id));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(new ConfirmTotpResult(backupCodes));
    }

    /// <summary>8 xonali raqamli zaxira kod — kiritish oson, `RandomNumberGenerator` bilan kriptografik tasodifiy.</summary>
    private static string GenerateBackupCode() =>
        RandomNumberGenerator.GetInt32(0, 100_000_000).ToString("D8", CultureInfo.InvariantCulture);
}
