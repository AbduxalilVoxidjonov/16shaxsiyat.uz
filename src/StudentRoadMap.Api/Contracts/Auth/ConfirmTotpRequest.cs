using StudentRoadMap.Application.Identity.ConfirmTotp;

namespace StudentRoadMap.Api.Contracts.Auth;

/// <summary>
/// `POST /api/auth/totp/confirm` so'rov tanasi — `{code}`: autentifikator ilovasidagi joriy
/// 6 xonali kod (`docs/07` 2-bo'lim).
/// </summary>
public sealed record ConfirmTotpRequest(string Code)
{
    public ConfirmTotpCommand ToCommand(Guid adminUserId) => new(adminUserId, Code);
}
