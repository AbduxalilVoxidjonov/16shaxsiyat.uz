using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// Superadmin JWT access tokenini yaratish (`docs/08-auth-va-xavfsizlik.md` 2-bo'lim: `sub`,
/// `name`, `role`, `jti`, `iat`, `exp` claim'lari, HS256, `Jwt:Key`). `Infrastructure`da amalga
/// oshiriladi — implementatsiya konstruktorida `Jwt:Key` uzunligini tekshiradi (≥ 32 bayt,
/// aks holda fail-fast, `docs/13-auth-va-jwt.md` MAXSUS DIQQAT 2-band).
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Access token yaratadi va uning amal qilish muddatini qaytaradi.</summary>
    JwtAccessToken CreateAccessToken(AdminUser user, DateTimeOffset now);
}

/// <summary>Yaratilgan access token va uning amal qilish muddati (soniyalarda, `docs/07` `expiresIn`).</summary>
public sealed record JwtAccessToken(string Token, DateTimeOffset ExpiresAt, int ExpiresInSeconds);
