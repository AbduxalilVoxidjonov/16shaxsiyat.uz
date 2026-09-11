using FluentAssertions;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Domain.Tests.Students;

public sealed class StudentTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly PhoneNumber Phone = PhoneNumber.Create("901234567").Value;

    private static Student CreateStudent(int grade = 5, DateTimeOffset? consentGivenAt = null) => Student.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "  Aliyev   Sardor ",
        new DateOnly(2012, 3, 1),
        Gender.Male,
        grade,
        Phone,
        consentGivenAt ?? Now,
        Now);

    [Fact]
    public void Create_NormalizesFullNameIntoNormalizedName()
    {
        var student = CreateStudent();

        student.NormalizedName.Should().Be("ALIYEV SARDOR");
    }

    [Fact]
    public void Create_WithEmptyFullName_ThrowsArgumentException()
    {
        var act = () => Student.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "   ",
            new DateOnly(2012, 3, 1),
            Gender.Male,
            5,
            Phone,
            Now,
            Now);

        act.Should().Throw<ArgumentException>();
    }

    // `0` bu ro'yxatdan CHIQARILDI — endi u `Student.NoGrade` (maktabda o'qimaydigan
    // ommaviy foydalanuvchi), quyidagi alohida testda tekshiriladi.
    [Theory]
    [InlineData(12)]
    [InlineData(-1)]
    [InlineData(100)]
    public void Create_WithGradeOutsideValidRange_ThrowsArgumentOutOfRangeException(int grade)
    {
        var act = () => CreateStudent(grade);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(11)]
    public void Create_WithBoundaryGrade_Succeeds(int grade)
    {
        var student = CreateStudent(grade);

        student.Grade.Should().Be(grade);
    }

    [Fact]
    public void Create_WithDefaultConsentGivenAt_ThrowsArgumentException()
    {
        var act = () => CreateStudent(consentGivenAt: default(DateTimeOffset));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateSnapshot_SetsSnapshotFieldsAndUpdatedAt()
    {
        var student = CreateStudent();
        var later = Now.AddDays(3);

        student.UpdateSnapshot(
            "INTJ",
            75.5,
            60.2,
            ActivityLevel.Active,
            "IRA",
            needsAttention: true,
            lastAssessmentAt: later,
            completedAssessmentCount: 2,
            now: later);

        student.LastPersonalityType.Should().Be("INTJ");
        student.LastMaturityIndex.Should().Be(75.5);
        student.LastActivityIndex.Should().Be(60.2);
        student.LastActivityLevel.Should().Be(ActivityLevel.Active);
        student.LastHollandCode.Should().Be("IRA");
        student.NeedsAttention.Should().BeTrue();
        student.LastAssessmentAt.Should().Be(later);
        student.CompletedAssessmentCount.Should().Be(2);
        student.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void MarkDeleted_SetsIsDeletedAndDeletedAt()
    {
        var student = CreateStudent();
        var deletedAt = Now.AddDays(5);

        student.MarkDeleted(deletedAt);

        student.IsDeleted.Should().BeTrue();
        student.DeletedAt.Should().Be(deletedAt);
    }

    // --- P52 (2026-09-11): `RegistrationMode.None` — anonim o'quvchi (`docs/18` §9). ---

    [Fact]
    public void CreateAnonymous_SetsIsAnonymousAndNullIdentityFields()
    {
        var id = Guid.NewGuid();

        var student = Student.CreateAnonymous(id, Guid.NewGuid(), consentGivenAt: Now, now: Now);

        student.IsAnonymous.Should().BeTrue();
        student.BirthDate.Should().BeNull();
        student.Phone.Should().BeNull();
        student.Grade.Should().Be(Student.NoGrade);
        student.Gender.Should().Be(Gender.Unspecified);
        student.FullName.Should().StartWith("Anonim ishtirokchi #");
        student.FullName.Should().NotContain(id.ToString());
    }

    [Fact]
    public void CreateAnonymous_TwoCalls_ProduceDifferentFullNames()
    {
        var first = Student.CreateAnonymous(Guid.NewGuid(), Guid.NewGuid(), Now, Now);
        var second = Student.CreateAnonymous(Guid.NewGuid(), Guid.NewGuid(), Now, Now);

        first.FullName.Should().NotBe(second.FullName, "admin ro'yxatida qatorlar bir-biridan ajralib turishi kerak");
    }

    [Fact]
    public void Create_NonAnonymous_AlwaysHasBirthDateAndPhone()
    {
        var student = CreateStudent();

        student.IsAnonymous.Should().BeFalse();
        student.BirthDate.Should().NotBeNull();
        student.Phone.Should().NotBeNull();
    }
}
