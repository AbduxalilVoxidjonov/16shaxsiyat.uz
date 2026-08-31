using FluentAssertions;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Domain.Tests.Students;

public sealed class NameNormalizerTests
{
    [Fact]
    public void Normalize_WithExtraWhitespaceAndLowercase_MatchesCleanUppercaseForm()
    {
        var messy = NameNormalizer.Normalize("  Alisher   Navoiy ");
        var clean = NameNormalizer.Normalize("alisher navoiy");

        messy.Should().Be(clean);
        messy.Should().Be("ALISHER NAVOIY");
    }

    [Theory]
    [InlineData("Sardor o'g'li", "'")]
    [InlineData("Sardor o’g’li", "’")]
    [InlineData("Sardor oʻgʻli", "ʻ")]
    [InlineData("Sardor oʼgʼli", "ʼ")]
    [InlineData("Sardor o`g`li", "`")]
    public void Normalize_WithDifferentApostropheVariants_ProducesSameResult(string name, string apostrophe)
    {
        _ = apostrophe; // faqat hujjatlashtirish uchun — Theory nomi aniqroq bo'lsin

        var result = NameNormalizer.Normalize(name);

        result.Should().Be("SARDOR O'G'LI");
    }

    [Fact]
    public void Normalize_WithNullInput_ThrowsArgumentNullException()
    {
        var act = () => NameNormalizer.Normalize(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Normalize_WithSingleName_TrimsAndUppercases()
    {
        NameNormalizer.Normalize("  aziz  ").Should().Be("AZIZ");
    }
}
