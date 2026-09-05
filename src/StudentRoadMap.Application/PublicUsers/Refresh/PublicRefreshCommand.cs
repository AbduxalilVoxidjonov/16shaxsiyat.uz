using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.PublicUsers.Refresh;

/// <summary>
/// `POST /api/auth/telegram/refresh` — tana bo'sh; refresh token kontroller tomonidan
/// `httpOnly` cookie'dan (`srm_public_refresh_token`) o'qilib shu yerga uzatiladi
/// (`RefreshCommand` bilan bir xil naqsh).
/// </summary>
public sealed record PublicRefreshCommand(string? RawRefreshToken, string? IpAddress) : IRequest<Result<PublicRefreshResult>>;
