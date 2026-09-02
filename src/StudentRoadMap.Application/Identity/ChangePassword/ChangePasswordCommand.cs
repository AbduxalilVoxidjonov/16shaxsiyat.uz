using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Identity.ChangePassword;

/// <summary>
/// `POST /api/auth/change-password` — `docs/07-api-shartnoma.md` 2-bo'lim:
/// `{currentPassword, newPassword}`. Muvaffaqiyatda BARCHA refresh tokenlar bekor qilinadi
/// (`docs/13-auth-va-jwt.md` MAXSUS DIQQAT 7-band).
/// </summary>
public sealed record ChangePasswordCommand(Guid AdminUserId, string CurrentPassword, string NewPassword) : IRequest<Result>;
