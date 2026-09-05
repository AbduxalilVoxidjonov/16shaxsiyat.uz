using System.Text.Json.Serialization;

namespace StudentRoadMap.Application.PublicUsers.TelegramLogin;

/// <summary>
/// `docs/07` 1a-bo'lim javob shakli: `{accessToken, expiresIn, user}`. Refresh token
/// `httpOnly` cookie'da (`LoginResult` bilan AYNAN bir xil naqsh — `[JsonIgnore]` tufayli
/// javob tanasiga hech qachon tushmaydi).
/// </summary>
public sealed record TelegramLoginResult(
    string AccessToken,
    int ExpiresIn,
    PublicUserDto User)
{
    [JsonIgnore]
    public string RefreshToken { get; init; } = string.Empty;

    [JsonIgnore]
    public DateTimeOffset RefreshTokenExpiresAt { get; init; }

    /// <summary>Bu kirishda YANGI akkaunt yaratildimi (frontend "xush kelibsiz" ekrani uchun).</summary>
    public bool IsNewUser { get; init; }
}

/// <summary>
/// Ommaviy foydalanuvchi profili. `TelegramId` ATAYLAB YO'Q — u tashqi tizim identifikatori,
/// mijozga qaytarilishi hech qanday funksiyani ochmaydi, lekin akkauntlarni bog'lash uchun
/// yaroqli ma'lumot bo'lardi (`docs/08` 5-bo'lim, minimallik prinsipi).
/// </summary>
public sealed record PublicUserDto(
    Guid Id,
    string? Username,
    string? FirstName,
    string? LastName,
    string? PhotoUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastLoginAt);
