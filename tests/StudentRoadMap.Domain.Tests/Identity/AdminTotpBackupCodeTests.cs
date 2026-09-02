using FluentAssertions;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Domain.Tests.Identity;

public sealed class AdminTotpBackupCodeTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithEmptyAdminUserId_ThrowsArgumentException()
    {
        var act = () => AdminTotpBackupCode.Create(Guid.NewGuid(), Guid.Empty, "hash", Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyHash_ThrowsArgumentException()
    {
        var act = () => AdminTotpBackupCode.Create(Guid.NewGuid(), Guid.NewGuid(), "  ", Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_SetsFieldsAndIsNotUsed()
    {
        var code = AdminTotpBackupCode.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", Now);

        code.IsUsed.Should().BeFalse();
        code.UsedAt.Should().BeNull();
        code.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void MarkUsed_SetsUsedAt()
    {
        var code = AdminTotpBackupCode.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", Now);

        code.MarkUsed(Now.AddMinutes(5));

        code.IsUsed.Should().BeTrue();
        code.UsedAt.Should().Be(Now.AddMinutes(5));
    }

    [Fact]
    public void MarkUsed_WhenAlreadyUsed_ThrowsDomainException()
    {
        var code = AdminTotpBackupCode.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", Now);
        code.MarkUsed(Now.AddMinutes(5));

        var act = () => code.MarkUsed(Now.AddMinutes(10));

        act.Should().Throw<DomainException>().Where(e => e.Code == "TOTP_BACKUP_CODE_ALREADY_USED");
    }
}
