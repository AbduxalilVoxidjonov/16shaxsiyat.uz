using MediatR;
using StudentRoadMap.Application.PublicUsers.TelegramLogin;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.PublicUsers.GetProfile;

/// <summary>
/// `GET /api/me` — joriy ommaviy foydalanuvchi profili. `PublicUserId` mijozdan EMAS,
/// JWT `sub` claim'idan keladi (kontroller `ICurrentUser` orqali to'ldiradi) — `CLAUDE.md`
/// 8-qoida ruhida: egalik hech qachon so'rov tanasi/URL bilan aniqlanmaydi.
/// </summary>
public sealed record GetMyProfileQuery(Guid PublicUserId) : IRequest<Result<PublicUserDto>>;
