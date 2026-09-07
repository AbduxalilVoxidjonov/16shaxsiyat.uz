using FluentAssertions;
using StudentRoadMap.Domain.Events;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Domain.Tests.Schools;

public sealed class SchoolTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static School CreateSchool() => School.Create(
        Guid.NewGuid(),
        "12-son maktab",
        "Farg'ona",
        "Qo'qon",
        SchoolSlug.Create("12-son-maktab-qoqon").Value,
        "initial-access-token",
        "ABCD2345",
        Now);

    [Fact]
    public void Create_WithValidData_SetsDefaultsAndIsActive()
    {
        var school = CreateSchool();

        school.IsActive.Should().BeTrue();
        school.IsDeleted.Should().BeFalse();
        school.DailyRegistrationLimit.Should().Be(500);
        school.CreatedAt.Should().Be(Now);
        school.UpdatedAt.Should().Be(Now);
    }

    [Theory]
    [InlineData("", "Farg'ona", "Qo'qon")]
    [InlineData("Nomi", "", "Qo'qon")]
    [InlineData("Nomi", "Farg'ona", "")]
    public void Create_WithMissingRequiredField_ThrowsArgumentException(string name, string region, string district)
    {
        var act = () => School.Create(
            Guid.NewGuid(),
            name,
            region,
            district,
            SchoolSlug.Create("nomi").Value,
            "token",
            "ABCD2345",
            Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNonPositiveDailyRegistrationLimit_ThrowsArgumentOutOfRangeException()
    {
        var act = () => School.Create(
            Guid.NewGuid(),
            "Nomi",
            "Farg'ona",
            "Qo'qon",
            SchoolSlug.Create("nomi").Value,
            "token",
            "ABCD2345",
            Now,
            dailyRegistrationLimit: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RegenerateAccessToken_ReplacesOldTokenAndRaisesEvent()
    {
        var school = CreateSchool();
        var oldToken = school.AccessToken;
        var later = Now.AddDays(1);

        school.RegenerateAccessToken("brand-new-token", later);

        school.AccessToken.Should().NotBe(oldToken);
        school.AccessToken.Should().Be("brand-new-token");
        school.UpdatedAt.Should().Be(later);
        school.DomainEvents.Should().ContainSingle(e => e is SchoolLinkRegeneratedEvent);
    }

    [Fact]
    public void RegenerateAccessToken_WithEmptyToken_ThrowsArgumentException()
    {
        var school = CreateSchool();

        var act = () => school.RegenerateAccessToken("   ", Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var school = CreateSchool();

        school.Deactivate(Now.AddHours(1));

        school.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsIsActiveTrue()
    {
        var school = CreateSchool();
        school.Deactivate(Now.AddHours(1));

        school.Activate(Now.AddHours(2));

        school.IsActive.Should().BeTrue();
    }

    [Fact]
    public void MarkDeleted_SetsIsDeletedAndDeletedAt()
    {
        var school = CreateSchool();
        var deletedAt = Now.AddDays(10);

        school.MarkDeleted(deletedAt);

        school.IsDeleted.Should().BeTrue();
        school.DeletedAt.Should().Be(deletedAt);
    }

    [Fact]
    public void Restore_AfterMarkDeleted_ClearsDeletedState()
    {
        var school = CreateSchool();
        school.MarkDeleted(Now.AddDays(10));

        school.Restore(Now.AddDays(11));

        school.IsDeleted.Should().BeFalse();
        school.DeletedAt.Should().BeNull();
    }
}
