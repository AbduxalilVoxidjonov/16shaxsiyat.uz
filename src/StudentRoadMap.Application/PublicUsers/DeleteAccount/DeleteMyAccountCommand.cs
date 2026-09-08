using MediatR;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.PublicUsers;

namespace StudentRoadMap.Application.PublicUsers.DeleteAccount;

/// <summary>
/// `DELETE /api/me` — foydalanuvchi o'z akkauntini o'chiradi (`PublicUser.MarkDeleted`:
/// anonimlashtirish). Idempotent. `Reason` MAJBURIY (validatorda tekshiriladi — `null` bo'lsa
/// so'rov `PublicUser` allaqachon o'chirilgan bo'lsa ham handlerga yetib bormaydi, `400`
/// qaytadi); `Comment` ixtiyoriy, `Reason == Other` bo'lsa MAJBURIY.
/// </summary>
public sealed record DeleteMyAccountCommand(
    Guid PublicUserId,
    PublicUserDeletionReason? Reason,
    string? Comment,
    string? IpAddress) : IRequest<Result>;
