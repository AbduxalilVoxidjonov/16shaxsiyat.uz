using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Identity.Common;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Identity.Login;

/// <summary>
/// `docs/07` 2-bo'lim + `docs/08` 2-bo'lim + `docs/13-auth-va-jwt.md` MAXSUS DIQQAT.
///
/// **Timing-attack himoyasi (MAXSUS DIQQAT 1-band):** mavjud bo'lmagan foydalanuvchi va
/// noto'g'ri parol bir xil javob ("Login yoki parol noto'g'ri") va bir xil vaqt sarflashi
/// kerak. Buning uchun foydalanuvchi topilmasa ham <see cref="IPasswordHasher.Verify"/>
/// xuddi shunday (soxta, lekin format jihatidan haqiqiy) xesh bilan chaqiriladi — PBKDF2ning
/// 210 000 iteratsiyali CPU xarajati ikkala yo'lda ham bir xil bo'ladi. Bloklangan hisob (423)
/// va TOTP talab qilinishi (401 `TOTP_REQUIRED`) BU qoidadan chetga chiqadi — ular allaqachon
/// to'g'ri login/parol topilganidan keyingi, tabiiy ravishda farqlanadigan bosqichlar.
/// </summary>
internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    private const int RefreshTokenByteLength = 64;
    private const int MaxUserAgentLength = 300;

    // Format jihatidan HAQIQIY PBKDF2 xeshiga o'xshaydi (`{iterations}.{saltBase64}.{hashBase64}`)
    // — shu sabab `Pbkdf2PasswordHasher.Verify` erta qaytmay, to'liq 210k iteratsiyali hisoblashni
    // bajaradi (timing parity). Tarkibi ahamiyatsiz — baribir hech qanday parolga mos kelmaydi.
    private static readonly string DummyPasswordHash =
        $"210000.{Convert.ToBase64String(new byte[16])}.{Convert.ToBase64String(new byte[32])}";

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IIpHasher _ipHasher;
    private readonly IAppSettings _appSettings;
    private readonly IEncryptionService _encryptionService;
    private readonly ITotpService _totpService;

    public LoginCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ITokenGenerator tokenGenerator,
        IIpHasher ipHasher,
        IAppSettings appSettings,
        IEncryptionService encryptionService,
        ITotpService totpService)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _tokenGenerator = tokenGenerator;
        _ipHasher = ipHasher;
        _appSettings = appSettings;
        _encryptionService = encryptionService;
        _totpService = totpService;
    }

    public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;
        var ipHash = _ipHasher.Hash(request.IpAddress);
        var userAgent = Truncate(request.UserAgent);
        var normalizedUsername = request.Username.Trim();

        var user = await _executor.FirstOrDefaultAsync(
            _context.AdminUsers.Where(u => u.Username.ToLower() == normalizedUsername.ToLower()),
            cancellationToken).ConfigureAwait(false);

        // Bloklangan hisob — parolni tekshirmasdan darhol rad etiladi (allaqachon "hisob
        // mavjud" ma'lumoti oshkor, timing-attack qoidasi bu yerga tegishli emas).
        if (user is not null && user.IsLocked(now))
        {
            _context.Add(AuditLog.Create(AuditActions.AuthLoginFailed, now, user.Id, ipHash: ipHash, userAgent: userAgent, afterJson: "{\"reason\":\"locked\"}"));
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result.Failure<LoginResult>(new Error(ProblemCodes.AccountLocked, "Hisob vaqtincha bloklangan. Birozdan so'ng qayta urinib ko'ring."));
        }

        var passwordValid = _passwordHasher.Verify(request.Password, user?.PasswordHash ?? DummyPasswordHash);

        if (user is null || !passwordValid || !user.IsActive)
        {
            user?.RegisterFailedLogin(now);
            _context.Add(AuditLog.Create(AuditActions.AuthLoginFailed, now, user?.Id, ipHash: ipHash, userAgent: userAgent));
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result.Failure<LoginResult>(InvalidCredentialsError());
        }

        if (user.TotpEnabled)
        {
            if (string.IsNullOrWhiteSpace(request.TotpCode))
            {
                return Result.Failure<LoginResult>(new Error(ProblemCodes.TotpRequired, "Tasdiqlash kodi kiritilishi shart."));
            }

            var totpValid = await ValidateTotpAsync(user, request.TotpCode, now, cancellationToken).ConfigureAwait(false);
            if (!totpValid)
            {
                user.RegisterFailedLogin(now);
                _context.Add(AuditLog.Create(AuditActions.AuthLoginFailed, now, user.Id, ipHash: ipHash, userAgent: userAgent, afterJson: "{\"reason\":\"totp\"}"));
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return Result.Failure<LoginResult>(new Error(ProblemCodes.Unauthorized, "Tasdiqlash kodi noto'g'ri."));
            }
        }

        user.ResetFailedLogins(now);

        var accessToken = _jwtTokenService.CreateAccessToken(user, now);
        var rawRefreshToken = _tokenGenerator.GenerateUrlSafeToken(RefreshTokenByteLength);
        var refreshExpiresAt = now.AddDays(_appSettings.RefreshTokenDays);
        var refreshToken = RefreshToken.Create(
            Guid.NewGuid(),
            user.Id,
            RefreshTokenHash.Compute(rawRefreshToken),
            refreshExpiresAt,
            now,
            ipHash);

        _context.Add(refreshToken);
        _context.Add(AuditLog.Create(AuditActions.AuthLoginSucceeded, now, user.Id, ipHash: ipHash, userAgent: userAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var result = new LoginResult(accessToken.Token, accessToken.ExpiresInSeconds, ToDto(user))
        {
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
        };

        return Result.Success(result);
    }

    /// <summary>
    /// QA topilmasi (`docs/13-auth-va-jwt.md`): ikki bir vaqtdagi so'rov bir xil kod bilan
    /// kelsa, ikkalasi ham "o'qi, keyin yoz" oynasida validatsiyadan o'tishi mumkin edi.
    ///
    /// Asosiy kod (`RegisterTotpStepUsed`) — `user.ConcurrencyStamp` orqali himoyalangan:
    /// ikkala so'rov ham `user`ni bir xil boshlang'ich holatda yuklaydi va o'zgartiradi, lekin
    /// `SaveChangesAsync` faqat BITTASINI qabul qiladi (ikkinchisi `ConcurrencyConflictException`
    /// oladi — `AppDbContext.SaveChangesAsync`/`AdminUserConfiguration` izohiga qarang).
    ///
    /// Zaxira kod — `IAppDbContext.TryMarkTotpBackupCodeUsedAsync` orqali ATOMIK (`UPDATE ...
    /// WHERE used_at IS NULL`, `IncrementRegistrationCounterAsync`dagi bilan bir xil naqsh);
    /// domendagi `AdminTotpBackupCode.MarkUsed()` ATAYLAB ishlatilmaydi — u EF tracked
    /// "o'qi-tekshir-yoz" bo'lardi va xuddi shu poyga holatiga ochiq qolardi.
    /// </summary>
    private async Task<bool> ValidateTotpAsync(AdminUser user, string code, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(user.TotpSecretEncrypted))
        {
            var secret = _encryptionService.Decrypt(user.TotpSecretEncrypted);
            if (_totpService.TryValidate(secret, code, now, user.TotpLastUsedStep, out var matchedStep))
            {
                user.RegisterTotpStepUsed(matchedStep, now);
                return true;
            }
        }

        // Asosiy kod mos kelmadi — zaxira kodlar orasidan qidiramiz (bir martalik, `docs/08` 2-bo'lim).
        var backupCodes = await _executor.ToListAsync(
            _context.AdminTotpBackupCodes.Where(c => c.AdminUserId == user.Id && c.UsedAt == null),
            cancellationToken).ConfigureAwait(false);

        foreach (var backupCode in backupCodes)
        {
            if (_passwordHasher.Verify(code, backupCode.CodeHash))
            {
                // Atomik shartli UPDATE — `false` qaytsa kod boshqa bir vaqtdagi so'rov
                // tomonidan ALLAQACHON ishlatilgan (poyga holati yopilgan).
                return await _context.TryMarkTotpBackupCodeUsedAsync(backupCode.Id, now, cancellationToken).ConfigureAwait(false);
            }
        }

        return false;
    }

    private static Error InvalidCredentialsError() =>
        new(ProblemCodes.Unauthorized, "Login yoki parol noto'g'ri.");

    private static AdminUserDto ToDto(AdminUser user) =>
        new(user.Id, user.Username, user.Email, user.FullName, user.Role.ToString(), user.TotpEnabled);

    private static string? Truncate(string? value) =>
        string.IsNullOrEmpty(value) || value.Length <= MaxUserAgentLength ? value : value[..MaxUserAgentLength];
}
