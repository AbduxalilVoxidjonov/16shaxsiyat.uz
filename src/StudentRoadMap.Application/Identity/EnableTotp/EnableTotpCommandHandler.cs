using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Identity.EnableTotp;

/// <summary>
/// O'rnatishning 1-bosqichi. **2FA bu yerda YOQILMAYDI** — sir faqat kutish holatiga yoziladi.
/// Ilgari bu handler `user.EnableTotp(...)` ni darhol chaqirar edi: agar foydalanuvchi 32
/// belgili sirni qo'lda xato ko'chirsa (QR yo'q edi), 2FA server tomonda yoqilib qolar va
/// keyingi kirishda hisob butunlay bloklanardi. Endi ilova to'g'ri kod berayotgani
/// isbotlanmaguncha login oqimi o'zgarmaydi.
/// </summary>
internal sealed class EnableTotpCommandHandler : IRequestHandler<EnableTotpCommand, Result<EnableTotpResult>>
{
    private const string IssuerName = "Shaxsiyat";

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IEncryptionService _encryptionService;
    private readonly ITotpService _totpService;
    private readonly IQrCodeGenerator _qrCodeGenerator;

    public EnableTotpCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        IEncryptionService encryptionService,
        ITotpService totpService,
        IQrCodeGenerator qrCodeGenerator)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _encryptionService = encryptionService;
        _totpService = totpService;
        _qrCodeGenerator = qrCodeGenerator;
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

        // Takroriy chaqiruv oldingi tasdiqlanmagan sirni almashtiradi — foydalanuvchi QR'ni
        // qayta so'raganda eski, yarim skanerlangan sir qolib ketmasin.
        user.BeginTotpEnrollment(_encryptionService.Encrypt(secret), now);

        // Sir hech qachon audit logga yozilmaydi — faqat "o'rnatish boshlandi" fakti.
        _context.Add(AuditLog.Create(AuditActions.AuthTotpEnrollmentStarted, now, user.Id));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var otpauthUri = _totpService.BuildOtpauthUri(secret, user.Username, IssuerName);

        return Result.Success(new EnableTotpResult(
            secret,
            otpauthUri,
            _qrCodeGenerator.GeneratePngBase64(otpauthUri),
            now.Add(AdminUser.PendingTotpEnrollmentLifetime)));
    }
}
