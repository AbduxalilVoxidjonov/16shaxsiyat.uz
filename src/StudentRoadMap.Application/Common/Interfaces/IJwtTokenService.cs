using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// JWT access tokenlarini yaratish. IKKI auditoriya bor va ular ATAYLAB alohida metodlarga
/// ajratilgan (bitta "generic" metod EMAS):
///
/// • <see cref="CreateAccessToken"/> — superadmin (`docs/08` 2-bo'lim: `sub`, `name`, `role`,
///   `jti`, `iat`, `exp`, HS256, `aud = Jwt:Audience`). O'ZGARMAYDI.
/// • <see cref="CreatePublicUserAccessToken"/> — ommaviy (Telegram) foydalanuvchi
///   (`sub` = `PublicUser.Id`, `role = PublicUser`, `aud = Jwt:PublicAudience`).
///
/// Ikki metod ikki xil `aud` beradi — shu bilan "ommaviy token superadmin endpointiga"
/// (va aksincha) holati IMZO/validatsiya darajasida imkonsiz bo'ladi, faqat rolga
/// tayanilmaydi (`PublicUserClaims` izohiga qarang).
///
/// **PII yo'q:** ommaviy tokenda `name`/`username`/ism claim'i ATAYLAB BERILMAYDI
/// (`CLAUDE.md` 5-qoida ruhida — token log/proxy/brauzer tarixida qolishi mumkin;
/// profil ma'lumoti faqat `GET /api/me` orqali, autentifikatsiyadan keyin beriladi).
///
/// `Infrastructure`da amalga oshiriladi — konstruktorda `Jwt:Key` uzunligi tekshiriladi
/// (≥ 32 bayt, aks holda fail-fast, `docs/13-auth-va-jwt.md` MAXSUS DIQQAT 2-band).
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Superadmin access tokeni va uning amal qilish muddati.</summary>
    JwtAccessToken CreateAccessToken(AdminUser user, DateTimeOffset now);

    /// <summary>
    /// Ommaviy foydalanuvchi access tokeni. Faqat identifikator uzatiladi — `PublicUser`
    /// agregatining o'zi kerak emas (tokenga hech qanday profil maydoni tushmaydi).
    /// </summary>
    JwtAccessToken CreatePublicUserAccessToken(Guid publicUserId, DateTimeOffset now);
}

/// <summary>Yaratilgan access token va uning amal qilish muddati (soniyalarda, `docs/07` `expiresIn`).</summary>
public sealed record JwtAccessToken(string Token, DateTimeOffset ExpiresAt, int ExpiresInSeconds);
