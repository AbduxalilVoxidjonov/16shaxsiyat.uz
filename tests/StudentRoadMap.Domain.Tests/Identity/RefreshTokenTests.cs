using FluentAssertions;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Domain.Tests.Identity;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithExpiresAtNotAfterNow_ThrowsArgumentException()
    {
        var act = () => RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", Now, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsActive_BeforeExpiryAndNotRevoked_ReturnsTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", Now.AddDays(30), Now);

        token.IsActive(Now.AddDays(1)).Should().BeTrue();
    }

    [Fact]
    public void IsActive_AfterExpiry_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", Now.AddDays(30), Now);

        token.IsActive(Now.AddDays(31)).Should().BeFalse();
    }

    [Fact]
    public void Revoke_SetsRevokedAtAndMakesTokenInactive()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", Now.AddDays(30), Now);

        token.Revoke(Now.AddDays(1));

        token.RevokedAt.Should().Be(Now.AddDays(1));
        token.IsActive(Now.AddDays(2)).Should().BeFalse();
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ThrowsDomainException()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", Now.AddDays(30), Now);
        token.Revoke(Now.AddDays(1));

        var act = () => token.Revoke(Now.AddDays(2));

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("REFRESH_TOKEN_ALREADY_REVOKED");
    }
}
