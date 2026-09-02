using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Identity.Common;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Identity.Refresh;

/// <summary>
/// `docs/08-auth-va-xavfsizlik.md` 2-bo'lim: rotatsiya (har chaqiruvda eskisi bekor, yangisi
/// beriladi) + qayta ishlatishni aniqlash. **Qayta ishlatish** — bekor qilingan (`RevokedAt`
/// bor) token bilan qaytadan urinish: bu o'g'irlangan token belgisi (hujumchi eski nusxani
/// ishlatgan, haqiqiy foydalanuvchi esa rotatsiyalangan yangisini allaqachon ishlatgan/ishlatadi)
/// — foydalanuvchining BARCHA refresh tokenlari bekor qilinadi + `Security.RefreshReuse` audit
/// (`docs/13-auth-va-jwt.md` MAXSUS DIQQAT 3-band).
/// </summary>
internal sealed class RefreshCommandHandler : IRequestHandler<RefreshCommand, Result<RefreshResult>>
{
    private const int RefreshTokenByteLength = 64;

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IIpHasher _ipHasher;
    private readonly IAppSettings _appSettings;

    public RefreshCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        IJwtTokenService jwtTokenService,
        ITokenGenerator tokenGenerator,
        IIpHasher ipHasher,
        IAppSettings appSettings)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _jwtTokenService = jwtTokenService;
        _tokenGenerator = tokenGenerator;
        _ipHasher = ipHasher;
        _appSettings = appSettings;
    }

    public async Task<Result<RefreshResult>> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;
        var ipHash = _ipHasher.Hash(request.IpAddress);

        if (string.IsNullOrWhiteSpace(request.RawRefreshToken))
        {
            return Result.Failure<RefreshResult>(InvalidTokenError());
        }

        var tokenHash = RefreshTokenHash.Compute(request.RawRefreshToken);

        var token = await _executor.FirstOrDefaultAsync(
            _context.RefreshTokens.Where(t => t.TokenHash == tokenHash),
            cancellationToken).ConfigureAwait(false);

        if (token is null)
        {
            return Result.Failure<RefreshResult>(InvalidTokenError());
        }

        if (token.RevokedAt is not null)
        {
            // Qayta ishlatish — foydalanuvchining BARCHA faol refresh tokenlarini bekor qilamiz.
            var activeTokens = await _executor.ToListAsync(
                _context.RefreshTokens.Where(t => t.AdminUserId == token.AdminUserId && t.RevokedAt == null),
                cancellationToken).ConfigureAwait(false);

            foreach (var activeToken in activeTokens)
            {
                activeToken.Revoke(now);
            }

            _context.Add(AuditLog.Create(AuditActions.SecurityRefreshReuse, now, token.AdminUserId, ipHash: ipHash));
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result.Failure<RefreshResult>(InvalidTokenError());
        }

        if (!token.IsActive(now))
        {
            // Muddati o'tgan (bekor qilinmagan, lekin `ExpiresAt` o'tgan) — endi ishlatib bo'lmaydi,
            // ammo qayta ishlatishni aniqlash mexanizmi keyingi urinishni ushlashi uchun bekor qilib qo'yamiz.
            token.Revoke(now);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result.Failure<RefreshResult>(InvalidTokenError());
        }

        var user = await _executor.FirstOrDefaultAsync(
            _context.AdminUsers.Where(u => u.Id == token.AdminUserId),
            cancellationToken).ConfigureAwait(false);

        if (user is null || !user.IsActive)
        {
            token.Revoke(now);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result.Failure<RefreshResult>(InvalidTokenError());
        }

        // Rotatsiya: eskisi bekor, yangisi yaratiladi.
        token.Revoke(now);

        var rawRefreshToken = _tokenGenerator.GenerateUrlSafeToken(RefreshTokenByteLength);
        var refreshExpiresAt = now.AddDays(_appSettings.RefreshTokenDays);
        var newToken = RefreshToken.Create(
            Guid.NewGuid(),
            user.Id,
            RefreshTokenHash.Compute(rawRefreshToken),
            refreshExpiresAt,
            now,
            ipHash);

        _context.Add(newToken);

        var accessToken = _jwtTokenService.CreateAccessToken(user, now);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var result = new RefreshResult(accessToken.Token, accessToken.ExpiresInSeconds)
        {
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
        };

        return Result.Success(result);
    }

    private static Error InvalidTokenError() =>
        new(ProblemCodes.Unauthorized, "Sessiya yaroqsiz. Iltimos, qaytadan kiring.");
}
