using FluentAssertions;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Domain.Tests.Scoring;

/// <summary>`docs/04-domain-model.md` §4: `PersonalityType`, `HollandCode`, `ScorePercent`.</summary>
public sealed class ScoringValueObjectsTests
{
    [Fact]
    public void PersonalityType_Create_WithValidLetters_BuildsCode()
    {
        var type = PersonalityType.Create('i', 'n', 't', 'j');

        type.Code.Should().Be("INTJ");
    }

    [Theory]
    [InlineData('X', 'N', 'T', 'J')]
    [InlineData('I', 'X', 'T', 'J')]
    [InlineData('I', 'N', 'X', 'J')]
    [InlineData('I', 'N', 'T', 'X')]
    public void PersonalityType_Create_WithInvalidLetter_Throws(char ei, char sn, char tf, char jp)
    {
        var act = () => PersonalityType.Create(ei, sn, tf, jp);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("INVALID_PERSONALITY_TYPE");
    }

    [Fact]
    public void PersonalityType_FromCode_WithWrongLength_Throws()
    {
        var act = () => PersonalityType.FromCode("INT");

        act.Should().Throw<DomainException>().Which.Code.Should().Be("INVALID_PERSONALITY_TYPE");
    }

    [Fact]
    public void PersonalityType_Equality_IsValueBased()
    {
        PersonalityType.Create('I', 'N', 'T', 'J').Should().Be(PersonalityType.Create('I', 'N', 'T', 'J'));
    }

    [Fact]
    public void HollandCode_Create_WithValidLetters_BuildsCode()
    {
        var code = HollandCode.Create(['i', 'r', 'a']);

        code.Code.Should().Be("IRA");
    }

    [Fact]
    public void HollandCode_Create_WithInvalidLetter_Throws()
    {
        var act = () => HollandCode.Create(['I', 'R', 'X']);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("INVALID_HOLLAND_CODE");
    }

    [Fact]
    public void HollandCode_Create_WithDuplicateLetters_Throws()
    {
        var act = () => HollandCode.Create(['I', 'R', 'I']);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("INVALID_HOLLAND_CODE");
    }

    [Fact]
    public void HollandCode_Create_WithTooManyLetters_Throws()
    {
        var act = () => HollandCode.Create(['R', 'I', 'A', 'S']);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("INVALID_HOLLAND_CODE");
    }

    [Theory]
    [InlineData(150.0, 100.0)]
    [InlineData(-20.0, 0.0)]
    [InlineData(55.555, 55.56)]
    [InlineData(50.0, 50.0)]
    public void ScorePercent_FromClamped_ClampsAndRounds(double rawValue, double expected)
    {
        ScorePercent.FromClamped(rawValue).Value.Should().Be(expected);
    }

    [Fact]
    public void ScorePercent_ImplicitDoubleConversion_ReturnsValue()
    {
        double value = ScorePercent.FromClamped(42.0);

        value.Should().Be(42.0);
    }
}
