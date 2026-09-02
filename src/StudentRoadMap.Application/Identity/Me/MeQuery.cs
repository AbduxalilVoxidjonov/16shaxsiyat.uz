using MediatR;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Identity.Me;

/// <summary>`GET /api/auth/me` — `docs/07-api-shartnoma.md` 2-bo'lim. Joriy foydalanuvchi.</summary>
public sealed record MeQuery(Guid AdminUserId) : IRequest<Result<AdminUserDto>>;
