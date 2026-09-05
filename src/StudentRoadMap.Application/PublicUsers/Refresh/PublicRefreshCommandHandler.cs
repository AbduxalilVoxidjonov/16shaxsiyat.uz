using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.PublicUsers.Common;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.PublicUsers;

namespace StudentRoadMap.Application.PublicUsers.Refresh;

/// <summary>
/// `Identity/Refresh/RefreshCommandHandler` NAQSHINI AYNAN takrorlaydi (`docs/08` 2-bo'lim):
/// rotatsiya (har chaqiruvda eskisi bekor, yangisi beriladi) + **qayta ishlatishni aniqlash**
/// — bekor qilingan token bilan qaytadan urinish o'g'irlik belgisi, foydalanuvchining BARCHA
/// refresh tokenlari bekor qilinadi va audit yoziladi.
///
/// Yagona farqlar: jadval (`PublicRefreshTokens`), egasi (`PublicUsers`), audit harakati
/// (`PublicSecurity.RefreshReuse`) va access tokenning `aud`i. Mantiq va rad javoblari
/// (har doim bir xil generik `401`) BIR XIL — ikki oqim orasida xatti-harakat farqi bo'lmasligi
/// uchun ataylab.
/// </summary>
internal sealed class PublicRefreshCommandHandler : IRequestHandler<PublicRefreshCommand, Result<PublicRefreshResult>>
{
    private const int RefreshTokenByteLength = 64;

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IIpHasher _ipHasher;
    private readonly IAppSettings _appSettings;

    public PublicRefreshCommandHandler(
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

    public async Task<Result<PublicRefreshResult>> Handle(PublicRefreshCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;
        var ipHash = _ipHasher.Hash(request.IpAddress);

        if (string.IsNullOrWhiteSpace(request.RawRefreshToken))
        {
            return Result.Failure<PublicRefreshResult>(InvalidTokenError());
        }

        var tokenHash = TokenHash.Compute(request.RawRefreshToken);

        var token = await _executor.FirstOrDefaultAsync(
            _context.PublicRefreshTokens.Where(t => t.TokenHash == tokenHash),
            cancellationToken).ConfigureAwait(false);

        if (token is null)
        {
            return Result.Failure<PublicRefreshResult>(InvalidTokenError());
        }

        if (token.RevokedAt is not null)
        {
            // Qayta ishlatish — foydalanuvchining BARCHA faol refresh tokenlarini bekor qilamiz.
            var activeTokens = await _executor.ToListAsync(
                _context.PublicRefreshTokens.Where(t => t.PublicUserId == token.PublicUserId && t.RevokedAt == null),
                cancellationToken).ConfigureAwait(false);

            foreach (var activeToken in activeTokens)
            {
                activeToken.Revoke(now);
            }

            _context.Add(AuditLog.Create(
                PublicAuditActions.RefreshReuse,
                now,
                entityType: PublicAuditActions.PublicUserEntityType,
                entityId: token.PublicUserId,
                ipHash: ipHash));

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result.Failure<PublicRefreshResult>(InvalidTokenError());
        }

        if (!token.IsActive(now))
        {
            // Muddati o'tgan (bekor qilinmagan, lekin `ExpiresAt` o'tgan) — endi ishlatib
            // bo'lmaydi, ammo qayta ishlatishni aniqlash mexanizmi keyingi urinishni ushlashi
            // uchun bekor qilib qo'yamiz.
            token.Revoke(now);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result.Failure<PublicRefreshResult>(InvalidTokenError());
        }

        // Global filtr o'chirilgan akkauntni yashiradi — ya'ni "ma'lumotimni o'chiring"dan
        // keyin qolgan refresh token bilan tiklanib bo'lmaydi.
        var user = await _executor.FirstOrDefaultAsync(
            _context.PublicUsers.Where(u => u.Id == token.PublicUserId),
            cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            token.Revoke(now);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result.Failure<PublicRefreshResult>(InvalidTokenError());
        }

        // Rotatsiya: eskisi bekor, yangisi yaratiladi.
        token.Revoke(now);

        var rawRefreshToken = _tokenGenerator.GenerateUrlSafeToken(RefreshTokenByteLength);
        var refreshExpiresAt = now.AddDays(_appSettings.RefreshTokenDays);

        _context.Add(PublicRefreshToken.Create(
            Guid.NewGuid(),
            user.Id,
            TokenHash.Compute(rawRefreshToken),
            refreshExpiresAt,
            now,
            ipHash));

        var accessToken = _jwtTokenService.CreatePublicUserAccessToken(user.Id, now);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var result = new PublicRefreshResult(accessToken.Token, accessToken.ExpiresInSeconds)
        {
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
        };

        return Result.Success(result);
    }

    private static Error InvalidTokenError() =>
        new(ProblemCodes.Unauthorized, "Sessiya yaroqsiz. Iltimos, qaytadan kiring.");
}
