using FluentAssertions;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.PublicUsers;

namespace StudentRoadMap.Domain.Tests.PublicUsers;

/// <summary>
/// `RefreshTokenTests` (superadmin) bilan AYNAN bir xil ssenariylar — ikki jadval
/// bir xil xavfsizlik qoidalariga bo'ysunishini qulflaydi.
/// </summary>
public sealed class PublicRefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_MuddatiOtganTokenBilan_ArgumentExceptionOtadi()
    {
        var act = () => PublicRefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", Now, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_BoshXeshBilan_ArgumentExceptionOtadi()
    {
        var act = () => PublicRefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "  ", Now.AddDays(14), Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsActive_MuddatIchidaVaBekorQilinmagan_True()
    {
        var token = PublicRefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", Now.AddDays(14), Now);

        token.IsActive(Now.AddDays(1)).Should().BeTrue();
    }

    [Fact]
    public void IsActive_MuddatOtgach_False()
    {
        var token = PublicRefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", Now.AddDays(14), Now);

        token.IsActive(Now.AddDays(15)).Should().BeFalse();
    }

    [Fact]
    public void Revoke_TokenniOldiradi()
    {
        var token = PublicRefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", Now.AddDays(14), Now);

        token.Revoke(Now.AddDays(1));

        token.RevokedAt.Should().Be(Now.AddDays(1));
        token.IsActive(Now.AddDays(2)).Should().BeFalse();
    }

    [Fact]
    public void Revoke_IkkinchiMarta_DomainExceptionOtadi()
    {
        // Qayta-ishlatishni aniqlash (`RefreshCommandHandler` naqshi) aynan shu holatga tayanadi.
        var token = PublicRefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", Now.AddDays(14), Now);
        token.Revoke(Now.AddDays(1));

        var act = () => token.Revoke(Now.AddDays(2));

        act.Should().Throw<DomainException>().Which.Code.Should().Be("PUBLIC_REFRESH_TOKEN_ALREADY_REVOKED");
    }

    [Fact]
    public void Create_FKVaIpXeshiniSaqlaydi()
    {
        var publicUserId = Guid.NewGuid();

        var token = PublicRefreshToken.Create(Guid.NewGuid(), publicUserId, "hash", Now.AddDays(14), Now, createdByIpHash: "ip-hash");

        token.PublicUserId.Should().Be(publicUserId);
        token.CreatedByIpHash.Should().Be("ip-hash");
        token.CreatedAt.Should().Be(Now);
    }
}
