using FluentAssertions;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Domain.Tests.Schools;

/// <summary>
/// Ommaviy makon (`SchoolKind.PublicSpace`) domen kafolatlari — P47.
/// DB tomonidagi ikkinchi qatlam (`ux_schools_public_space` qisman unikal indeks)
/// `Migrations.Tests` da tekshiriladi.
/// </summary>
public sealed class SchoolPublicSpaceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static School CreatePublicSpace() => School.CreatePublicSpace(
        Guid.NewGuid(),
        "Ommaviy makon",
        SchoolSlug.FromExisting("ommaviy"),
        "public-space-token",
        Now);

    [Fact]
    public void Create_OddiyMaktab_KindSchoolVaNatijaYopiq()
    {
        var school = School.Create(
            Guid.NewGuid(),
            "12-son maktab",
            "Farg'ona",
            "Qo'qon",
            SchoolSlug.Create("12-son-maktab").Value,
            "token",
            "ABCD2345",
            Now);

        school.Kind.Should().Be(SchoolKind.School);
        school.IsPublicSpace.Should().BeFalse();
        school.ShowResultToStudent.Should().BeFalse("mavjud maktablar uchun standart qiymat eski global sozlamadagidek (`false`) qoladi");
    }

    [Fact]
    public void CreatePublicSpace_NatijaniKorsatishYoqilganVaFaol()
    {
        var space = CreatePublicSpace();

        space.Kind.Should().Be(SchoolKind.PublicSpace);
        space.IsPublicSpace.Should().BeTrue();
        space.ShowResultToStudent.Should().BeTrue("tashqi foydalanuvchi o'z natijasini ko'rmasa kabinetning ma'nosi yo'q");
        space.IsActive.Should().BeTrue();
        space.IsDeleted.Should().BeFalse();
        space.AccessCode.Should().BeNull();
        space.DailyRegistrationLimit.Should().Be(School.DefaultPublicSpaceRegistrationLimit);
    }

    [Fact]
    public void CreatePublicSpace_BoshNom_ArgumentExceptionOtadi()
    {
        var act = () => School.CreatePublicSpace(Guid.NewGuid(), "  ", SchoolSlug.FromExisting("ommaviy"), "token", Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreatePublicSpace_BoshToken_ArgumentExceptionOtadi()
    {
        var act = () => School.CreatePublicSpace(Guid.NewGuid(), "Ommaviy makon", SchoolSlug.FromExisting("ommaviy"), "  ", Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_OmmaviyMakonda_DomainExceptionOtadi()
    {
        var space = CreatePublicSpace();

        var act = () => space.Deactivate(Now.AddDays(1));

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCHOOL_PUBLIC_SPACE_PROTECTED");
        space.IsActive.Should().BeTrue();
    }

    [Fact]
    public void MarkDeleted_OmmaviyMakonda_DomainExceptionOtadi()
    {
        var space = CreatePublicSpace();

        var act = () => space.MarkDeleted(Now.AddDays(1));

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCHOOL_PUBLIC_SPACE_PROTECTED");
        space.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void DeactivateVaMarkDeleted_OddiyMaktabda_IshlaydiOzgarishsiz()
    {
        var school = School.Create(
            Guid.NewGuid(), "12-son maktab", "Farg'ona", "Qo'qon", SchoolSlug.Create("12-son").Value, "token", "ABCD2345", Now);

        school.Deactivate(Now.AddDays(1));
        school.IsActive.Should().BeFalse();

        school.MarkDeleted(Now.AddDays(2));
        school.IsDeleted.Should().BeTrue();
        school.DeletedAt.Should().Be(Now.AddDays(2));
    }

    [Fact]
    public void SetShowResultToStudent_BayroqniOzgartiradiVaUpdatedAtniYangilaydi()
    {
        var school = School.Create(
            Guid.NewGuid(), "12-son maktab", "Farg'ona", "Qo'qon", SchoolSlug.Create("12-son").Value, "token", "ABCD2345", Now);

        school.SetShowResultToStudent(true, Now.AddDays(3));

        school.ShowResultToStudent.Should().BeTrue();
        school.UpdatedAt.Should().Be(Now.AddDays(3));
    }

    [Fact]
    public void UpdateDetails_ShowResultToStudentGaTegmaydi()
    {
        var space = CreatePublicSpace();

        space.UpdateDetails("Yangi nom", "Ommaviy", "Ommaviy", null, null, null, null, 1000, null, Now.AddDays(1));

        space.ShowResultToStudent.Should().BeTrue("bayroq mustaqil metod orqali boshqariladi");
        space.Kind.Should().Be(SchoolKind.PublicSpace, "tur ma'muriy tahrirda o'zgarmaydi");
    }
}
