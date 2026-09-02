using StudentRoadMap.Application.Identity.DisableTotp;

namespace StudentRoadMap.Api.Contracts.Auth;

/// <summary>`POST /api/auth/totp/disable` so'rov tanasi — joriy parol qayta so'raladi (nozik amal).</summary>
public sealed record DisableTotpRequest(string CurrentPassword)
{
    public DisableTotpCommand ToCommand(Guid adminUserId) => new(adminUserId, CurrentPassword);
}
