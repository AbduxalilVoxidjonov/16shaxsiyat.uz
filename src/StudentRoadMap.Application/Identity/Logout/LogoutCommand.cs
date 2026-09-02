using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Identity.Logout;

/// <summary>
/// `POST /api/auth/logout` — `docs/07-api-shartnoma.md` 2-bo'lim. Refresh tokenni bekor qiladi;
/// idempotent — token topilmasa/allaqachon bekor qilingan bo'lsa ham xato bermaydi (cookie
/// kontroller tomonidan har doim tozalanadi).
/// </summary>
public sealed record LogoutCommand(string? RawRefreshToken) : IRequest<Result>;
