using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.PublicUsers.DeleteAccount;

/// <summary>
/// `DELETE /api/me` — foydalanuvchi o'z akkauntini o'chiradi (`PublicUser.MarkDeleted`:
/// anonimlashtirish). Idempotent.
/// </summary>
public sealed record DeleteMyAccountCommand(Guid PublicUserId, string? IpAddress) : IRequest<Result>;
