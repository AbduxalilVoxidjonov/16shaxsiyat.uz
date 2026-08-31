using FluentAssertions;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Domain.Tests.Students;

public sealed class PhoneNumberTests
{
    private const string Expected = "+998901234567";

    [Theory]
    [InlineData("998901234567")]
    [InlineData("901234567")]
    [InlineData("+998 90 123 45 67")]
    [InlineData("(90) 123-45-67")]
    [InlineData("90-123-45-67")]
    public void Create_WithVariousFormats_NormalizesToCanonicalForm(string raw)
    {
        var result = PhoneNumber.Create(raw);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(Expected);
    }

    [Fact]
    public void Create_WithEmptyInput_ReturnsPhoneEmptyError()
    {
        var result = PhoneNumber.Create("");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PHONE_EMPTY");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("abcdefghi")]
    [InlineData("+1 234 567 8900")]
    [InlineData("99890123456")]
    public void Create_WithInvalidInput_ReturnsPhoneInvalidError(string raw)
    {
        var result = PhoneNumber.Create(raw);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PHONE_INVALID");
    }

    [Fact]
    public void ToString_ReturnsCanonicalValue()
    {
        var phone = PhoneNumber.Create("901234567").Value;

        phone.ToString().Should().Be(Expected);
    }
}
