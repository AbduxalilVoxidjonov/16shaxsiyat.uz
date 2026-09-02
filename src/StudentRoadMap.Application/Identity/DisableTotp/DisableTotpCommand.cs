using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Identity.DisableTotp;

/// <summary>
/// `POST /api/auth/totp/disable` — `docs/07-api-shartnoma.md` 2-bo'lim. Joriy parolni qayta
/// so'raydi (2FA'ni o'chirish nozik amal — o'g'irlangan qisqa muddatli access token bilan ham
/// oson o'chirilmasin degan maqsadda, `docs/13-auth-va-jwt.md` MAXSUS DIQQAT ruhida).
/// </summary>
public sealed record DisableTotpCommand(Guid AdminUserId, string CurrentPassword) : IRequest<Result>;
