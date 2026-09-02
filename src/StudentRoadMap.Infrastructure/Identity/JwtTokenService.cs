using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Infrastructure.Identity;

/// <summary>
/// Superadmin JWT access tokenini yaratadi — HS256, claim'lar `sub`/`name`/`role`/`jti`
/// (`docs/08-auth-va-xavfsizlik.md` 2-bo'lim). `Jwt:Key` kamida 32 bayt bo'lishi SHART —
/// aks holda konstruktorda (`AesEncryptionService` bilan bir xil naqsh) darhol xato beriladi.
/// `Program.cs` ilova ishga tushganda bu servisni MAJBURIY resolve qiladi — shu orqali
/// fail-fast birinchi login so'roviga emas, START-UPga bog'lanadi (`docs/13-auth-va-jwt.md`
/// MAXSUS DIQQAT 2-band).
/// </summary>
internal sealed class JwtTokenService : IJwtTokenService
{
    private const int MinKeyBytes = 32;
    private const int DefaultAccessTokenMinutes = 30;

    private readonly SymmetricSecurityKey _signingKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < MinKeyBytes)
        {
            throw new InvalidOperationException(
                $"'Jwt:Key' sozlamasi topilmadi yoki {MinKeyBytes} baytdan qisqa. Env o'zgaruvchi orqali kamida {MinKeyBytes} baytli tasodifiy qiymat bering (masalan: `openssl rand -base64 48`).");
        }

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        _issuer = configuration["Jwt:Issuer"] ?? "studentroadmap";
        _audience = configuration["Jwt:Audience"] ?? "studentroadmap-admin";

        var minutesRaw = configuration["Jwt:AccessTokenMinutes"];
        _accessTokenMinutes = int.TryParse(minutesRaw, out var minutes) && minutes > 0 ? minutes : DefaultAccessTokenMinutes;
    }

    public JwtAccessToken CreateAccessToken(AdminUser user, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(user);

        var expiresAt = now.AddMinutes(_accessTokenMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("name", user.Username),
            new Claim("role", user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256));

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new JwtAccessToken(tokenString, expiresAt, _accessTokenMinutes * 60);
    }
}
