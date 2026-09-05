using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.PublicUsers.Common;

namespace StudentRoadMap.Api.Auth;

/// <summary>
/// `ICurrentUser`ning JWT claim'lariga asoslangan amalga oshirilishi. `sub`/`role` claim
/// nomlari `JwtTokenService`/`JwtAuthenticationSetup`dagi bilan bir xil.
///
/// Ikkala JWT oqimi ham `sub` claim'ini ishlatadi (superadmin — `AdminUser.Id`, ommaviy —
/// `PublicUser.Id`), shu sabab <see cref="PublicUserId"/> QO'SHIMCHA ravishda `role` claim'ini
/// tekshiradi: aks holda superadmin tokeni bilan kelgan so'rov `PublicUserId` sifatida
/// admin identifikatorini qaytarardi. Teskarisi ham himoyalangan — ommaviy tokenda
/// `AdminUserId` `null` bo'ladi.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid? AdminUserId => Role == PublicUserClaims.Role ? null : Subject;

    public string? Role => _httpContextAccessor.HttpContext?.User.FindFirst("role")?.Value;

    public Guid? PublicUserId => Role == PublicUserClaims.Role ? Subject : null;

    /// <summary>`sub` claim — ikkala oqimda ham identifikator shu yerda.</summary>
    private Guid? Subject
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}
