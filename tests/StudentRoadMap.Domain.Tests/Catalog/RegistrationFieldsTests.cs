using FluentAssertions;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Domain.Tests.Catalog;

/// <summary>
/// `RegistrationFields` — qiymat obyekti va uning batareya invarianti (P52 kengaytmasi,
/// `docs/18` §9.5). `RegistrationFieldsJson` — (de)serializatsiya shakli.
/// </summary>
public sealed class RegistrationFieldsTests
{
    [Fact]
    public void Default_MatchesDocumentedTable()
    {
        RegistrationFields.Default.BirthDate.Should().Be(RegistrationFieldRequirement.Required);
        RegistrationFields.Default.Gender.Should().Be(RegistrationFieldRequirement.Required);
        RegistrationFields.Default.Grade.Should().Be(RegistrationFieldRequirement.Required);
        RegistrationFields.Default.ClassLetter.Should().Be(RegistrationFieldRequirement.Optional);
        RegistrationFields.Default.Phone.Should().Be(RegistrationFieldRequirement.Required);
        RegistrationFields.Default.ParentPhone.Should().Be(RegistrationFieldRequirement.Optional);
        RegistrationFields.Default.Email.Should().Be(RegistrationFieldRequirement.Optional);
    }

    [Fact]
    public void SatisfiesPersonalityBatteryInvariant_Default_IsTrue()
    {
        RegistrationFields.Default.SatisfiesPersonalityBatteryInvariant().Should().BeTrue();
    }

    [Theory]
    [InlineData(RegistrationFieldRequirement.Optional)]
    [InlineData(RegistrationFieldRequirement.Hidden)]
    public void SatisfiesPersonalityBatteryInvariant_NonRequiredBirthDate_IsFalse(RegistrationFieldRequirement value)
    {
        var fields = RegistrationFields.Default with { BirthDate = value };

        fields.SatisfiesPersonalityBatteryInvariant().Should().BeFalse();
    }

    [Theory]
    [InlineData(RegistrationFieldRequirement.Optional)]
    [InlineData(RegistrationFieldRequirement.Hidden)]
    public void SatisfiesPersonalityBatteryInvariant_NonRequiredGrade_IsFalse(RegistrationFieldRequirement value)
    {
        var fields = RegistrationFields.Default with { Grade = value };

        fields.SatisfiesPersonalityBatteryInvariant().Should().BeFalse();
    }

    [Fact]
    public void SatisfiesPersonalityBatteryInvariant_GenderIgnored()
    {
        // `Gender` invariantga KIRMAYDI — `Hidden` bo'lsa ham batareya invarianti buzilmaydi.
        var fields = RegistrationFields.Default with { Gender = RegistrationFieldRequirement.Hidden };

        fields.SatisfiesPersonalityBatteryInvariant().Should().BeTrue();
    }

    [Fact]
    public void Json_RoundTrips_WithCamelCaseAndStringEnums()
    {
        var fields = RegistrationFields.Default with { BirthDate = RegistrationFieldRequirement.Optional, Grade = RegistrationFieldRequirement.Hidden };

        var json = RegistrationFieldsJson.Serialize(fields);

        json.Should().Contain("\"birthDate\":\"Optional\"");
        json.Should().Contain("\"grade\":\"Hidden\"");

        var roundTripped = RegistrationFieldsJson.Deserialize(json);

        roundTripped.Should().Be(fields);
    }

    [Fact]
    public void Json_Serialize_Null_ReturnsNull()
    {
        RegistrationFieldsJson.Serialize(null).Should().BeNull();
    }

    [Fact]
    public void Json_Deserialize_NullOrEmpty_ReturnsNull()
    {
        RegistrationFieldsJson.Deserialize(null).Should().BeNull();
        RegistrationFieldsJson.Deserialize(string.Empty).Should().BeNull();
    }
}
