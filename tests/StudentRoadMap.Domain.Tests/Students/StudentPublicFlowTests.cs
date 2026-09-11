using FluentAssertions;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Domain.Tests.Students;

/// <summary>
/// Ommaviy oqim uchun `Student` kengaytmalari — P47: akkaunt bog'lanishi, rozilik maydonlari,
/// "sinf yo'q" holati va yosh oralig'i (6–99).
/// </summary>
public sealed class StudentPublicFlowTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly PhoneNumber Phone = PhoneNumber.Create("901234567").Value;

    private static Student CreateStudent(
        int grade = 5,
        Guid? publicUserId = null,
        string? consentVersion = null,
        bool parentalConsent = false) => Student.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Aliyev Sardor",
            new DateOnly(2012, 3, 1),
            Gender.Male,
            grade,
            Phone,
            Now,
            Now,
            publicUserId: publicUserId,
            consentVersion: consentVersion,
            parentalConsent: parentalConsent);

    [Fact]
    public void Create_MaktabOqimida_PublicUserIdNullVaRozilikMaydonlariBosh()
    {
        var student = CreateStudent();

        student.PublicUserId.Should().BeNull("maktab havolasi oqimida akkaunt tushunchasi yo'q");
        student.ConsentVersion.Should().BeNull();
        student.ParentalConsent.Should().BeFalse();
        student.ConsentGivenAt.Should().Be(Now);
    }

    [Fact]
    public void Create_OmmaviyOqimda_AkkauntVaRozilikMaydonlariToldiriladi()
    {
        var publicUserId = Guid.NewGuid();

        var student = CreateStudent(publicUserId: publicUserId, consentVersion: "2026-09-v1", parentalConsent: true);

        student.PublicUserId.Should().Be(publicUserId);
        student.ConsentVersion.Should().Be("2026-09-v1");
        student.ParentalConsent.Should().BeTrue();
    }

    [Fact]
    public void Create_NoGradeBilan_Ishlaydi()
    {
        // Kattalar (talaba, ishchi) maktabda o'qimaydi — `Grade` ni nullable qilmasdan
        // 0 sentinel qiymati ishlatiladi (`Student.NoGrade`).
        var student = CreateStudent(grade: Student.NoGrade);

        student.Grade.Should().Be(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(12)]
    public void Create_NoGradeDanTashqariNotogriSinf_ArgumentOutOfRangeOtadi(int grade)
    {
        var act = () => CreateStudent(grade);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void YoshOraligi_Konstantalari_OltiDanToqsonToqqizgacha()
    {
        // `StartSessionCommandValidator` ilgari 6–20 talab qilardi — kattalar kira olmasdi.
        Student.MinAge.Should().Be(6);
        Student.MaxAge.Should().Be(99);
    }

    [Theory]
    [InlineData(2020, 3, 1, 5)]   // tug'ilgan kuni hali kelmagan
    [InlineData(2020, 1, 1, 6)]
    [InlineData(1996, 1, 1, 30)]
    public void CalculateAge_TugilganKunHisobgaOlinadi(int year, int month, int day, int expected)
    {
        var asOf = new DateOnly(2026, 1, 15);

        Student.CalculateAge(new DateOnly(year, month, day), asOf).Should().Be(expected);
    }

    [Theory]
    [InlineData(2020, 1, 1, true)]    // 6 yosh — quyi chegara
    [InlineData(2021, 1, 1, false)]   // 5 yosh
    [InlineData(1927, 1, 15, true)]   // 99 yosh — yuqori chegara
    [InlineData(1926, 1, 1, false)]   // 100 yosh
    [InlineData(2006, 1, 1, true)]    // 20 yoshdan katta — eski validator rad etardi
    public void IsAgeAllowed_ChegaralarniTogriTekshiradi(int year, int month, int day, bool expected)
    {
        var asOf = new DateOnly(2026, 1, 15);

        Student.IsAgeAllowed(new DateOnly(year, month, day), asOf).Should().Be(expected);
    }

    [Fact]
    public void LinkToPublicUser_BoglanmaganYozuvni_Boglaydi()
    {
        var student = CreateStudent();
        var publicUserId = Guid.NewGuid();

        student.LinkToPublicUser(publicUserId, Now.AddDays(1));

        student.PublicUserId.Should().Be(publicUserId);
        student.UpdatedAt.Should().Be(Now.AddDays(1));
    }

    [Fact]
    public void LinkToPublicUser_AynanShuAkkauntgaQaytaBoglash_Idempotent()
    {
        var publicUserId = Guid.NewGuid();
        var student = CreateStudent(publicUserId: publicUserId);

        var act = () => student.LinkToPublicUser(publicUserId, Now.AddDays(1));

        act.Should().NotThrow();
    }

    [Fact]
    public void LinkToPublicUser_BoshqaAkkauntgaBoglangan_DomainExceptionOtadi()
    {
        var student = CreateStudent(publicUserId: Guid.NewGuid());

        var act = () => student.LinkToPublicUser(Guid.NewGuid(), Now.AddDays(1));

        act.Should().Throw<DomainException>().Which.Code.Should().Be("STUDENT_ALREADY_LINKED");
    }

    [Fact]
    public void LinkToPublicUser_BoshIdentifikator_ArgumentExceptionOtadi()
    {
        var student = CreateStudent();

        var act = () => student.LinkToPublicUser(Guid.Empty, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RecordConsent_YangiVersiyaniYozadi()
    {
        var student = CreateStudent();
        var later = Now.AddDays(200);

        student.RecordConsent(later, "2027-01-v2", parentalConsent: true, now: later);

        student.ConsentGivenAt.Should().Be(later);
        student.ConsentVersion.Should().Be("2027-01-v2");
        student.ParentalConsent.Should().BeTrue();
        student.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void UpdateProfile_BarchaMaydonlarniYangilaydiVaNormalizedNameQaytaHisoblanadi()
    {
        var student = CreateStudent(grade: 9, consentVersion: "1.0", parentalConsent: true);
        var later = Now.AddDays(30);
        var newPhone = PhoneNumber.Create("+998911112233").Value;

        student.UpdateProfile("  Valiyeva   Malika ", new DateOnly(1995, 4, 12), Gender.Female, Student.NoGrade, newPhone, "  malika@example.com ", later);

        student.FullName.Should().Be("  Valiyeva   Malika ", "F.I.Sh. `Create` dagidek xom saqlanadi");
        student.NormalizedName.Should().Be("VALIYEVA MALIKA", "admin qidiruvi yangi F.I.Sh. bilan ishlashi kerak");
        student.BirthDate.Should().Be(new DateOnly(1995, 4, 12));
        student.Gender.Should().Be(Gender.Female);
        student.Grade.Should().Be(Student.NoGrade, "9-sinfdan \"maktabda o'qimayman\"ga o'tish mumkin");
        student.Phone.Should().Be(newPhone);
        student.Email.Should().Be("malika@example.com");
        student.UpdatedAt.Should().Be(later);

        // Rozilik maydonlari TEGILMAYDI — ular `RecordConsent` vakolatida.
        student.ConsentVersion.Should().Be("1.0");
        student.ParentalConsent.Should().BeTrue();
        student.ConsentGivenAt.Should().Be(Now);
    }

    [Fact]
    public void UpdateProfile_BoshEmail_NullQiladi()
    {
        var student = CreateStudent();

        student.UpdateProfile("Aliyev Sardor", student.BirthDate!.Value, student.Gender, student.Grade, Phone, "   ", Now);

        student.Email.Should().BeNull("bo'sh satr — emailni tozalash");
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("")]
    public void UpdateProfile_BoshFish_ArgumentExceptionOtadi(string fullName)
    {
        var student = CreateStudent();

        var act = () => student.UpdateProfile(fullName, student.BirthDate!.Value, student.Gender, student.Grade, Phone, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(12)]
    [InlineData(-1)]
    public void UpdateProfile_SinfChegaradanTashqarida_ArgumentOutOfRangeExceptionOtadi(int grade)
    {
        var student = CreateStudent();

        var act = () => student.UpdateProfile("Aliyev Sardor", student.BirthDate!.Value, student.Gender, grade, Phone, null, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RecordConsent_VaqtsizChaqiruv_ArgumentExceptionOtadi()
    {
        var student = CreateStudent();

        var act = () => student.RecordConsent(default, "v1", false, Now);

        act.Should().Throw<ArgumentException>();
    }
}
