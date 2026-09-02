using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Identity.Login;

/// <summary>
/// `POST /api/auth/login` — `docs/07-api-shartnoma.md` 2-bo'lim: `{username, password, totpCode?}`
/// → `{accessToken, expiresIn, user}`; refresh token `httpOnly` cookie'da (kontroller qo'shadi,
/// `LoginResult.RefreshToken` javob tanasiga hech qachon serializatsiya qilinmaydi).
/// `IpAddress`/`UserAgent` — mijozdan emas, kontroller `HttpContext`dan to'ldiradi
/// (`StartSessionCommand`dagi bir xil naqsh).
/// </summary>
public sealed record LoginCommand(
    string Username,
    string Password,
    string? TotpCode,
    string? IpAddress,
    string? UserAgent) : IRequest<Result<LoginResult>>;
