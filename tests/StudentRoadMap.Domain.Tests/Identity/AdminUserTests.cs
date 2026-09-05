using FluentAssertions;
using StudentRoadMap.Domain.Common;
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
        AdminUser.Create(Guid.NewGuid(), "superadmin", "admin@16shaxsiyat.uz", "hashed-password", Now);

    [Fact]
    public void Create_WithEmptyUsername_ThrowsArgumentException()
    {
        var act = () => AdminUser.Create(Guid.NewGuid(), "  ", "admin@16shaxsiyat.uz", "hash", Now);

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

    [Fact]
    public void RegisterTotpStepUsed_SetsLastUsedStep()
    {
        var admin = CreateAdminUser();

        admin.RegisterTotpStepUsed(12345, Now);

        admin.TotpLastUsedStep.Should().Be(12345);
    }

    // --- TOTP o'rnatishning ikki bosqichli oqimi (P46): `Begin...` 2FA'ni YOQMAYDI,
    // `Confirm...` esa faqat muddati o'tmagan kutish holatidan yoqadi. ---

    [Fact]
    public void BeginTotpEnrollment_KutishHolatigaYozadi_LekinTotpniYoqmaydi()
    {
        var admin = CreateAdminUser();

        admin.BeginTotpEnrollment("encrypted-secret", Now);

        admin.TotpEnabled.Should().BeFalse("tasdiqlanmaguncha 2FA yoqilmasligi kerak");
        admin.TotpSecretEncrypted.Should().BeNull();
        admin.PendingTotpSecretEncrypted.Should().Be("encrypted-secret");
        admin.PendingTotpCreatedAt.Should().Be(Now);
        admin.HasValidPendingTotpEnrollment(Now).Should().BeTrue();
    }

    [Fact]
    public void BeginTotpEnrollment_BoshSirBilan_ArgumentExceptionTashlaydi()
    {
        var admin = CreateAdminUser();

        var act = () => admin.BeginTotpEnrollment("  ", Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BeginTotpEnrollment_QaytaChaqirilganda_OldingiSirniAlmashtiradi()
    {
        var admin = CreateAdminUser();
        admin.BeginTotpEnrollment("first-secret", Now);

        admin.BeginTotpEnrollment("second-secret", Now.AddMinutes(2));

        admin.PendingTotpSecretEncrypted.Should().Be("second-secret");
        admin.PendingTotpCreatedAt.Should().Be(Now.AddMinutes(2));
    }

    [Fact]
    public void BeginTotpEnrollment_TotpYoqilganda_DomainExceptionTashlaydi()
    {
        var admin = CreateAdminUser();
        admin.BeginTotpEnrollment("encrypted-secret", Now);
        admin.ConfirmTotpEnrollment(Now.AddSeconds(30));

        var act = () => admin.BeginTotpEnrollment("another-secret", Now.AddMinutes(1));

        act.Should().Throw<DomainException>().Which.Code.Should().Be("TOTP_ALREADY_ENABLED");
    }

    [Fact]
    public void ConfirmTotpEnrollment_KutishHolatidagiSirniAsosiyGaKochiradi_VaYoqadi()
    {
        var admin = CreateAdminUser();
        admin.BeginTotpEnrollment("encrypted-secret", Now);

        admin.ConfirmTotpEnrollment(Now.AddMinutes(1));

        admin.TotpEnabled.Should().BeTrue();
        admin.TotpSecretEncrypted.Should().Be("encrypted-secret");
        admin.PendingTotpSecretEncrypted.Should().BeNull();
        admin.PendingTotpCreatedAt.Should().BeNull();
    }

    [Fact]
    public void ConfirmTotpEnrollment_OrnatishBoshlanmagan_DomainExceptionTashlaydi()
    {
        var admin = CreateAdminUser();

        var act = () => admin.ConfirmTotpEnrollment(Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("TOTP_ENROLLMENT_NOT_STARTED");
    }

    [Fact]
    public void ConfirmTotpEnrollment_MuddatiOtganda_DomainExceptionTashlaydi()
    {
        var admin = CreateAdminUser();
        admin.BeginTotpEnrollment("encrypted-secret", Now);
        var afterExpiry = Now.Add(AdminUser.PendingTotpEnrollmentLifetime).AddSeconds(1);

        admin.HasValidPendingTotpEnrollment(afterExpiry).Should().BeFalse();
        admin.HasExpiredPendingTotpEnrollment(afterExpiry).Should().BeTrue();

        var act = () => admin.ConfirmTotpEnrollment(afterExpiry);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("TOTP_ENROLLMENT_EXPIRED");
        admin.TotpEnabled.Should().BeFalse();
    }

    [Fact]
    public void CancelTotpEnrollment_KutishHolatiniTozalaydi()
    {
        var admin = CreateAdminUser();
        admin.BeginTotpEnrollment("encrypted-secret", Now);

        admin.CancelTotpEnrollment(Now.AddMinutes(1));

        admin.PendingTotpSecretEncrypted.Should().BeNull();
        admin.PendingTotpCreatedAt.Should().BeNull();
        admin.HasValidPendingTotpEnrollment(Now.AddMinutes(1)).Should().BeFalse();
        admin.HasExpiredPendingTotpEnrollment(Now.AddMinutes(1)).Should().BeFalse();
    }

    [Fact]
    public void DisableTotp_ClearsSecretEnabledFlagLastUsedStepAndPendingEnrollment()
    {
        var admin = CreateAdminUser();
        admin.BeginTotpEnrollment("encrypted-secret", Now);
        admin.ConfirmTotpEnrollment(Now);
        admin.RegisterTotpStepUsed(999, Now);

        admin.DisableTotp(Now.AddMinutes(1));

        admin.TotpEnabled.Should().BeFalse();
        admin.TotpSecretEncrypted.Should().BeNull();
        admin.TotpLastUsedStep.Should().BeNull();
        admin.PendingTotpSecretEncrypted.Should().BeNull();
        admin.PendingTotpCreatedAt.Should().BeNull();
    }
}
