using FluentAssertions;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Domain.Tests.Identity;

/// <summary>
/// `AdminUser` lockout siyosati — 5 ta noto'g'ri urinishdan keyin 15 daqiqaga bloklanadi
/// (`docs/08-auth-va-xavfsizlik.md`).
/// </summary>
public sealed class AdminUserTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static AdminUser CreateAdminUser() =>
        AdminUser.Create(Guid.NewGuid(), "superadmin", "admin@salohiyat.uz", "hashed-password", Now);

    [Fact]
    public void Create_WithEmptyUsername_ThrowsArgumentException()
    {
        var act = () => AdminUser.Create(Guid.NewGuid(), "  ", "admin@salohiyat.uz", "hash", Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RegisterFailedLogin_BelowThreshold_DoesNotLockAccount()
    {
        var admin = CreateAdminUser();

        for (var i = 0; i < AdminUser.MaxFailedLoginAttempts - 1; i++)
        {
            admin.RegisterFailedLogin(Now);
        }

        admin.FailedLoginCount.Should().Be(AdminUser.MaxFailedLoginAttempts - 1);
        admin.IsLocked(Now).Should().BeFalse();
    }

    [Fact]
    public void RegisterFailedLogin_AtThreshold_LocksAccountFor15Minutes()
    {
        var admin = CreateAdminUser();

        for (var i = 0; i < AdminUser.MaxFailedLoginAttempts; i++)
        {
            admin.RegisterFailedLogin(Now);
        }

        admin.FailedLoginCount.Should().Be(AdminUser.MaxFailedLoginAttempts);
        admin.IsLocked(Now).Should().BeTrue();
        admin.LockedUntil.Should().Be(Now.Add(AdminUser.LockoutDuration));
    }

    [Fact]
    public void IsLocked_JustBeforeLockoutExpires_ReturnsTrue()
    {
        var admin = CreateAdminUser();
        for (var i = 0; i < AdminUser.MaxFailedLoginAttempts; i++)
        {
            admin.RegisterFailedLogin(Now);
        }

        var justBeforeExpiry = Now.Add(AdminUser.LockoutDuration).AddTicks(-1);

        admin.IsLocked(justBeforeExpiry).Should().BeTrue();
    }

    [Fact]
    public void IsLocked_ExactlyAtLockoutExpiry_ReturnsFalse()
    {
        var admin = CreateAdminUser();
        for (var i = 0; i < AdminUser.MaxFailedLoginAttempts; i++)
        {
            admin.RegisterFailedLogin(Now);
        }

        var exactlyAtExpiry = Now.Add(AdminUser.LockoutDuration);

        admin.IsLocked(exactlyAtExpiry).Should().BeFalse();
    }

    [Fact]
    public void ResetFailedLogins_ClearsCountAndLockout()
    {
        var admin = CreateAdminUser();
        for (var i = 0; i < AdminUser.MaxFailedLoginAttempts; i++)
        {
            admin.RegisterFailedLogin(Now);
        }

        admin.ResetFailedLogins(Now.AddMinutes(20));

        admin.FailedLoginCount.Should().Be(0);
        admin.LockedUntil.Should().BeNull();
        admin.IsLocked(Now.AddMinutes(20)).Should().BeFalse();
        admin.LastLoginAt.Should().Be(Now.AddMinutes(20));
    }
}
