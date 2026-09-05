using System.Text.Json.Serialization;
using StudentRoadMap.Application.PublicUsers.TelegramLogin;

namespace StudentRoadMap.Api.Contracts.PublicUsers;

/// <summary>
/// `POST /api/auth/telegram` so'rov tanasi — Telegram Login Widget `onauth` callback'i
/// qaytargan obyekt AYNAN shu shaklda (o'zgartirilmasdan) yuboriladi.
///
/// **Maydon nomlari `snake_case` va bu MAJBURIY:** aynan shu kalitlar (`first_name`,
/// `photo_url`, `auth_date`) `data_check_string`ga tushadi — Telegram imzoni shu nomlar
/// bilan hisoblagan. Loyihaning qolgan qismi `camelCase` ishlatadi (`Program.cs`
/// `JsonSerializerDefaults.Web`), shu sabab bu yerda `[JsonPropertyName]` ANIQ beriladi —
/// aks holda frontend widget obyektini qayta nomlashga majbur bo'lardi va bitta xato nom
/// butun imzoni jimgina buzardi.
///
/// `IpAddress`/`UserAgent` bu yerda YO'Q — kontroller `HttpContext`dan to'ldiradi
/// (`StartSessionRequest` bilan bir xil naqsh).
/// </summary>
public sealed record TelegramLoginRequest(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("auth_date")] long AuthDate,
    [property: JsonPropertyName("hash")] string Hash,
    [property: JsonPropertyName("first_name")] string? FirstName = null,
    [property: JsonPropertyName("last_name")] string? LastName = null,
    [property: JsonPropertyName("username")] string? Username = null,
    [property: JsonPropertyName("photo_url")] string? PhotoUrl = null)
{
    public TelegramLoginCommand ToCommand(string? ipAddress, string? userAgent) =>
        new(Id, AuthDate, Hash, FirstName, LastName, Username, PhotoUrl, ipAddress, userAgent);
}
