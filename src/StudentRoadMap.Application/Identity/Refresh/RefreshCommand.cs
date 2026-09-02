using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Identity.Refresh;

/// <summary>
/// `POST /api/auth/refresh` — `docs/07-api-shartnoma.md` 2-bo'lim. Tana bo'sh; refresh token
/// kontroller tomonidan `httpOnly` cookie'dan o'qilib shu yerga uzatiladi (`docs/08` 2-bo'lim).
/// </summary>
public sealed record RefreshCommand(string? RawRefreshToken, string? IpAddress) : IRequest<Result<RefreshResult>>;
