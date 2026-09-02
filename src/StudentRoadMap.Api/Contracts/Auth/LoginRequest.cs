using StudentRoadMap.Application.Identity.Login;

namespace StudentRoadMap.Api.Contracts.Auth;

/// <summary>
/// `POST /api/auth/login` so'rov tanasi — `docs/07-api-shartnoma.md` 2-bo'lim:
/// `{username, password, totpCode?}`. `IpAddress`/`UserAgent` mijozdan kelmaydi — kontroller
/// `HttpContext`dan to'ldiradi (`StartSessionRequest`dagi bir xil naqsh).
/// </summary>
public sealed record LoginRequest(string Username, string Password, string? TotpCode)
{
    public LoginCommand ToCommand(string? ipAddress, string? userAgent) =>
        new(Username, Password, TotpCode, ipAddress, userAgent);
}
