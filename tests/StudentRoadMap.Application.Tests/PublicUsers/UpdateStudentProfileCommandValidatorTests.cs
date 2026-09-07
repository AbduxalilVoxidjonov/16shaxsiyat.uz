using FluentAssertions;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.PublicUsers.UpdateStudentProfile;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Tests.PublicUsers;

/// <summary>
/// `UpdateStudentProfileCommandValidator` — `PUT /api/me/profile` FORMAT qoidalari.
/// Qoidalar `StartPublicSessionCommandValidator` bilan BIR manbadan (`PublicProfileFormatRules`),
/// bu testlar shu bog'lanishni qulflaydi. Majburiylik handlerda (`UpdateMyProfileEndpointTests`).
/// </summary>
public sealed class UpdateStudentProfileCommandValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private static UpdateStudentProfileCommandValidator CreateValidator() => new(new FixedDateTime(Now));

    [Fact]
    public void BoshBuyruq_Otadi_MajburiylikHandlerda()
    {
        var result = CreateValidator().Validate(new UpdateStudentProfileCommand(Guid.NewGuid()));

        result.IsValid.Should().BeTrue("validator faqat KELGAN maydon formatini tekshiradi");
    }

    [Fact]
    public void ToliqTogriBuyruq_Otadi()
    {
        var command = new UpdateStudentProfileCommand(
            Guid.NewGuid(),
            "Karimov Sardor Alisherovich",
            new DateOnly(1990, 1, 1),
            Gender.Male,
            "+998901234567",
            ConsentAccepted: true,
            Grade: 0,
            Email: "sardor@example.com");

        CreateValidator().Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(2022)] // ~4 yosh — `Student.MinAge` dan kichik
    [InlineData(1900)] // ~126 yosh — `Student.MaxAge` dan katta
    public void YoshChegaradanTashqarida_Xato(int birthYear)
    {
        var result = CreateValidator().Validate(new UpdateStudentProfileCommand(Guid.NewGuid(), BirthDate: new DateOnly(birthYear, 1, 1)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateStudentProfileCommand.BirthDate));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(12)]
    public void SinfOraliqdanTashqarida_Xato(int grade)
    {
        var result = CreateValidator().Validate(new UpdateStudentProfileCommand(Guid.NewGuid(), Grade: grade));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateStudentProfileCommand.Grade));
    }

    [Fact]
    public void TelefonVaEmailFormati_Xato()
    {
        var result = CreateValidator().Validate(new UpdateStudentProfileCommand(Guid.NewGuid(), Phone: "12345", Email: "not-an-email"));

        result.Errors.Select(e => e.PropertyName).Should().BeEquivalentTo(
            nameof(UpdateStudentProfileCommand.Phone),
            nameof(UpdateStudentProfileCommand.Email));
    }

    [Fact]
    public void QisqaFish_Xato()
    {
        var result = CreateValidator().Validate(new UpdateStudentProfileCommand(Guid.NewGuid(), FullName: "Ali"));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateStudentProfileCommand.FullName));
    }

    private sealed class FixedDateTime : IDateTime
    {
        public FixedDateTime(DateTimeOffset now)
        {
            UtcNow = now;
        }

        public DateTimeOffset UtcNow { get; }
    }
}
