using System.Text.Json.Serialization;

namespace StudentRoadMap.Application.Identity.Login;

/// <summary>
/// `docs/07-api-shartnoma.md` 2-bo'lim javob shakli: `{accessToken, expiresIn, user}`.
/// `RefreshToken` — XOM (shifrlanmagan) yangilash tokeni, faqat kontroller uni `httpOnly`
/// cookie'ga yozish uchun o'qiydi; `[JsonIgnore]` bilan javob tanasiga HECH QACHON tushmaydi
/// (`docs/08-auth-va-xavfsizlik.md` 2-bo'lim: "refresh token faqat cookie'da").
/// </summary>
public sealed record LoginResult(
    string AccessToken,
    int ExpiresIn,
    AdminUserDto User)
{
    [JsonIgnore]
    public string RefreshToken { get; init; } = string.Empty;

    [JsonIgnore]
    public DateTimeOffset RefreshTokenExpiresAt { get; init; }
}

/// <summary>Javobdagi `user` obyekti — parol xeshi/TOTP siri hech qachon qo'shilmaydi.</summary>
public sealed record AdminUserDto(
    Guid Id,
    string Username,
    string Email,
    string? FullName,
    string Role,
    bool TotpEnabled);
