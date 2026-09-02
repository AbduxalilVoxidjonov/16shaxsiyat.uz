using FluentAssertions;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Domain.Tests.Identity;

public sealed class AuditLogTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithEmptyAction_ThrowsArgumentException()
    {
        var act = () => AuditLog.Create("  ", Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithMinimalArguments_SetsCreatedAtAndAction()
    {
        var log = AuditLog.Create(AuditActions.AuthLoginFailed, Now);

        log.Action.Should().Be(AuditActions.AuthLoginFailed);
        log.CreatedAt.Should().Be(Now);
        log.AdminUserId.Should().BeNull();
        log.EntityType.Should().BeNull();
        log.IpHash.Should().BeNull();
    }

    [Fact]
    public void Create_WithAllArguments_SetsEveryField()
    {
        var adminId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        var log = AuditLog.Create(
            AuditActions.SecurityRefreshReuse,
            Now,
            adminUserId: adminId,
            entityType: "RefreshToken",
            entityId: entityId,
            beforeJson: "{\"a\":1}",
            afterJson: "{\"a\":2}",
            ipHash: "hash",
            userAgent: "xunit");

        log.AdminUserId.Should().Be(adminId);
        log.EntityType.Should().Be("RefreshToken");
        log.EntityId.Should().Be(entityId);
        log.BeforeJson.Should().Be("{\"a\":1}");
        log.AfterJson.Should().Be("{\"a\":2}");
        log.IpHash.Should().Be("hash");
        log.UserAgent.Should().Be("xunit");
    }
}
