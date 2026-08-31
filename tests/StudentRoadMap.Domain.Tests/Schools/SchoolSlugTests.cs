using FluentAssertions;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Domain.Tests.Schools;

public sealed class SchoolSlugTests
{
    [Theory]
    [InlineData("12-son maktab, Qo'qon", "12-son-maktab-qoqon")]
    [InlineData("O'zbekiston", "ozbekiston")]
    [InlineData("G'ayrat maktabi", "gayrat-maktabi")]
    [InlineData("MAKTAB NOMI", "maktab-nomi")]
    [InlineData("  12  son   maktab  ", "12-son-maktab")]
    [InlineData("-maktab-", "maktab")]
    [InlineData("12--son__maktab", "12-son-maktab")]
    public void Create_NormalizesSourceToExpectedSlug(string source, string expectedSlug)
    {
        var result = SchoolSlug.Create(source);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(expectedSlug);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    [InlineData(",,,")]
    public void Create_WithEmptyOrUnusableSource_ReturnsFailure(string source)
    {
        var result = SchoolSlug.Create(source);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SLUG_EMPTY");
    }

    [Fact]
    public void Create_WithSourceLongerThanMaxLength_TruncatesAndTrimsTrailingDash()
    {
        var longSource = string.Join(" ", Enumerable.Repeat("maktab", 20)); // ancha uzun matn

        var result = SchoolSlug.Create(longSource);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Length.Should().BeLessThanOrEqualTo(SchoolSlug.MaxLength);
        result.Value.Value.Should().NotEndWith("-");
    }

    [Theory]
    [InlineData("12-son maktab, Qo'qon")]
    [InlineData("Тошкент 45-maktab")]
    [InlineData("Ø€Đ ¥¹²³")]
    public void Create_Result_OnlyContainsLowercaseLettersDigitsAndDashes(string source)
    {
        var result = SchoolSlug.Create(source);

        if (result.IsFailure)
        {
            return; // manba faqat ruxsat etilmagan belgilardan iborat bo'lishi mumkin
        }

        result.Value.Value.Should().MatchRegex("^[a-z0-9-]+$");
    }

    [Fact]
    public void FromExisting_WrapsValueWithoutRevalidation()
    {
        var slug = SchoolSlug.FromExisting("already-valid-slug");

        slug.Value.Should().Be("already-valid-slug");
    }

    [Fact]
    public void ToString_ReturnsUnderlyingValue()
    {
        var slug = SchoolSlug.Create("12-son maktab").Value;

        slug.ToString().Should().Be(slug.Value);
    }
}
