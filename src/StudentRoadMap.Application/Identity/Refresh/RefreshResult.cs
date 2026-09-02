using System.Text.Json.Serialization;

namespace StudentRoadMap.Application.Identity.Refresh;

/// <summary>Yangi access token; rotatsiya qilingan refresh token faqat cookie uchun (`[JsonIgnore]`).</summary>
public sealed record RefreshResult(string AccessToken, int ExpiresIn)
{
    [JsonIgnore]
    public string RefreshToken { get; init; } = string.Empty;

    [JsonIgnore]
    public DateTimeOffset RefreshTokenExpiresAt { get; init; }
}
