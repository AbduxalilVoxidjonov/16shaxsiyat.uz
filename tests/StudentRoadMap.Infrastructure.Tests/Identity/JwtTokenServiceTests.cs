using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Infrastructure.Identity;

namespace StudentRoadMap.Infrastructure.Tests.Identity;

/// <summary>
/// `JwtTokenService` — access token claim'lari (`sub`/`name`/`role`/`jti`) va `Jwt:Key` fail-fast
/// tekshiruvi (`docs/13-auth-va-jwt.md` MAXSUS DIQQAT 2-band).
/// </summary>
public sealed class JwtTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static IConfiguration BuildConfiguration(string? key = "0123456789abcdef0123456789abcdef", string? issuer = null, string? audience = null, string? minutes = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = key,
                ["Jwt:Issuer"] = issuer,
                ["Jwt:Audience"] = audience,
                ["Jwt:AccessTokenMinutes"] = minutes,
            })
            .Build();

    private static AdminUser CreateAdminUser() =>
        AdminUser.Create(Guid.NewGuid(), "superadmin", "admin@16shaxsiyat.uz", "hashed-password", Now);

    [Fact]
    public void Constructor_WithKeyShorterThan32Bytes_ThrowsInvalidOperationException()
    {
        var configuration = BuildConfiguration(key: "too-short-key");

        var act = () => new JwtTokenService(configuration);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_WithMissingKey_ThrowsInvalidOperationException()
    {
        var configuration = BuildConfiguration(key: null);

        var act = () => new JwtTokenService(configuration);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CreateAccessToken_ProducesTokenWithExpectedClaims()
    {
        var service = new JwtTokenService(BuildConfiguration(issuer: "srm-test", audience: "srm-test-audience"));
        var user = CreateAdminUser();

        var accessToken = service.CreateAccessToken(user, Now);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);

        jwt.Issuer.Should().Be("srm-test");
        jwt.Audiences.Should().Contain("srm-test-audience");
        jwt.Claims.Should().Contain(c => c.Type == "sub" && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "name" && c.Value == user.Username);
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == user.Role.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "jti");
        accessToken.ExpiresAt.Should().Be(Now.AddMinutes(30));
        accessToken.ExpiresInSeconds.Should().Be(30 * 60);
    }

    [Fact]
    public void CreateAccessToken_CalledTwice_ProducesDifferentJti()
    {
        var service = new JwtTokenService(BuildConfiguration());
        var user = CreateAdminUser();

        var first = new JwtSecurityTokenHandler().ReadJwtToken(service.CreateAccessToken(user, Now).Token);
        var second = new JwtSecurityTokenHandler().ReadJwtToken(service.CreateAccessToken(user, Now).Token);

        first.Claims.First(c => c.Type == "jti").Value.Should().NotBe(second.Claims.First(c => c.Type == "jti").Value);
    }

    [Fact]
    public void CreateAccessToken_WithCustomAccessTokenMinutes_RespectsConfiguration()
    {
        var service = new JwtTokenService(BuildConfiguration(minutes: "5"));
        var user = CreateAdminUser();

        var accessToken = service.CreateAccessToken(user, Now);

        accessToken.ExpiresAt.Should().Be(Now.AddMinutes(5));
        accessToken.ExpiresInSeconds.Should().Be(5 * 60);
    }
}
