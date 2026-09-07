using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.PublicUsers.GetStudentProfile;

/// <summary>
/// `GET /api/me/profile` — saqlangan anketa (`docs/07` §5.1a). `PublicUserId` JWT `sub`
/// claim'idan (kontroller `ICurrentUser` orqali) — mijozdan hech qachon emas.
/// </summary>
public sealed record GetMyStudentProfileQuery(Guid PublicUserId) : IRequest<Result<MyStudentProfileDto>>;
