using StudentRoadMap.Application.Identity.ChangePassword;

namespace StudentRoadMap.Api.Contracts.Auth;

/// <summary>`POST /api/auth/change-password` so'rov tanasi — `docs/07` 2-bo'lim: `{currentPassword, newPassword}`.</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword)
{
    public ChangePasswordCommand ToCommand(Guid adminUserId) => new(adminUserId, CurrentPassword, NewPassword);
}
