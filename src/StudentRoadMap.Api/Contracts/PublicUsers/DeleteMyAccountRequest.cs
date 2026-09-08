using StudentRoadMap.Application.PublicUsers.DeleteAccount;
using StudentRoadMap.Domain.PublicUsers;

namespace StudentRoadMap.Api.Contracts.PublicUsers;

/// <summary>
/// `DELETE /api/me` so'rov tanasi (egasining 2026-09-08 talabi — o'chirishdan oldin sabab
/// so'raladi). `Reason` MAJBURIY (`null`/berilmagan → `400 VALIDATION_ERROR`, validator);
/// noma'lum satr JSON deserializatsiya bosqichida allaqachon `400` beradi
/// (`JsonStringEnumConverter`). `Comment` ixtiyoriy, ≤500 belgi, `Reason == Other`da MAJBURIY.
/// </summary>
public sealed record DeleteMyAccountRequest(PublicUserDeletionReason? Reason, string? Comment)
{
    public DeleteMyAccountCommand ToCommand(Guid publicUserId, string? ipAddress) =>
        new(publicUserId, Reason, Comment, ipAddress);
}
