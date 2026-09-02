using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Api.Auth;

/// <summary>
/// `ICurrentUser`ning JWT (`Bearer`) claim'lariga asoslangan amalga oshirilishi — faqat admin
/// autentifikatsiyasi uchun (`ICurrentUser` interfeysi izohi). `sub`/`role` claim nomlari
/// `JwtTokenService`/`JwtAuthenticationSetup`dagi bilan bir xil.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid? AdminUserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Role => _httpContextAccessor.HttpContext?.User.FindFirst("role")?.Value;
}
