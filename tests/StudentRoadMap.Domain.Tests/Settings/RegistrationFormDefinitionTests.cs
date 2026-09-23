using FluentAssertions;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Settings;

namespace StudentRoadMap.Domain.Tests.Settings;

/// <summary>
/// `RegistrationFormDefinition` — GLOBAL ro'yxatdan o'tish formasi sozlamasining qiymati
/// (2026-09-11/12, egasining talabi, `docs/18` §9.6). Har bir validatsiya qoidasi uchun
/// kamida bitta test (topshiriq talabi).
/// </summary>
public sealed class RegistrationFormDefinitionTests
{
    [Fact]
    public void Default_MatchesDocumentedTable()
    {
        var d = RegistrationFormDefinition.Default;

        d.CoreFields.FullName.Requirement.Should().Be(RegistrationFieldRequirement.Required);
        d.CoreFields.FullName.LabelUz.Should().Be("F.I.Sh.");
        d.CoreFields.BirthDate.Requirement.Should().Be(RegistrationFieldRequirement.Required);
        d.CoreFields.Gender.Requirement.Should().Be(RegistrationFieldRequirement.Required);
        d.CoreFields.Grade.Requirement.Should().Be(RegistrationFieldRequirement.Required);
        d.CoreFields.ClassLetter.Requirement.Should().Be(RegistrationFieldRequirement.Optional);
        // 2026-09-23 egasi qarori: ota-ona telefoni birinchi va majburiy, o'z telefoni ixtiyoriy.
        d.CoreFields.ParentPhone.Requirement.Should().Be(RegistrationFieldRequirement.Required);
        d.CoreFields.ParentPhone.LabelUz.Should().Be("Ota-ona telefoni");
        d.CoreFields.ParentPhone.Order.Should().Be(6);
        d.CoreFields.Phone.Requirement.Should().Be(RegistrationFieldRequirement.Optional);
        d.CoreFields.Phone.LabelUz.Should().Be("Shaxsiy raqamingiz (bo'lsa)");
        d.CoreFields.Phone.Order.Should().Be(7);
        d.CoreFields.Email.Requirement.Should().Be(RegistrationFieldRequirement.Optional);
        d.CustomFields.Should().BeEmpty();
    }

    private static RegistrationCoreFields ValidCoreFields(RegistrationFieldRequirement fullNameRequirement = RegistrationFieldRequirement.Required) => new(
        FullName: new RegistrationCoreField(fullNameRequirement, "F.I.Sh.", null, 1),
        BirthDate: new RegistrationCoreField(RegistrationFieldRequirement.Required, "Tug'ilgan sana", null, 2),
        Gender: new RegistrationCoreField(RegistrationFieldRequirement.Required, "Jins", null, 3),
        Grade: new RegistrationCoreField(RegistrationFieldRequirement.Required, "Sinf", null, 4),
        ClassLetter: new RegistrationCoreField(RegistrationFieldRequirement.Optional, "Sinf harfi", null, 5),
        Phone: new RegistrationCoreField(RegistrationFieldRequirement.Required, "Telefon raqami", null, 6),
        ParentPhone: new RegistrationCoreField(RegistrationFieldRequirement.Optional, "Ota-ona telefoni", null, 7),
        Email: new RegistrationCoreField(RegistrationFieldRequirement.Optional, "Email", null, 8));

    [Fact]
    public void Create_ValidData_Succeeds()
    {
        var customFields = new List<RegistrationCustomField>
        {
            new("PARENT_JOB", QuestionType.ShortText, "Ota-onangiz kasbi", "Masalan: o'qituvchi", RegistrationFieldRequirement.Optional, 200, null, null, 9),
        };

        var definition = RegistrationFormDefinition.Create(ValidCoreFields(), customFields);

        definition.CustomFields.Should().ContainSingle(f => f.Code == "PARENT_JOB");
    }

    [Theory]
    [InlineData(RegistrationFieldRequirement.Optional)]
    [InlineData(RegistrationFieldRequirement.Hidden)]
    public void Create_FullNameNotRequired_Throws(RegistrationFieldRequirement value)
    {
        var act = () => RegistrationFormDefinition.Create(ValidCoreFields(value), []);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("REGISTRATION_FORM_FULL_NAME_LOCKED");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("has space")]
    [InlineData("has$symbol")]
    [InlineData("this-code-is-way-too-long-21")]
    public void Create_InvalidCustomFieldCode_Throws(string code)
    {
        var customFields = new List<RegistrationCustomField>
        {
            new(code, QuestionType.ShortText, "Test", null, RegistrationFieldRequirement.Optional, 200, null, null, 9),
        };

        var act = () => RegistrationFormDefinition.Create(ValidCoreFields(), customFields);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("REGISTRATION_FORM_FIELD_CODE_INVALID");
    }

    [Fact]
    public void Create_DuplicateCustomFieldCode_Throws()
    {
        var customFields = new List<RegistrationCustomField>
        {
            new("PARENT_JOB", QuestionType.ShortText, "Kasbi 1", null, RegistrationFieldRequirement.Optional, 200, null, null, 9),
            new("PARENT_JOB", QuestionType.ShortText, "Kasbi 2", null, RegistrationFieldRequirement.Optional, 200, null, null, 10),
        };

        var act = () => RegistrationFormDefinition.Create(ValidCoreFields(), customFields);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("REGISTRATION_FORM_FIELD_CODE_DUPLICATE");
    }

    [Theory]
    [InlineData("fullName")]
    [InlineData("birthDate")]
    [InlineData("gender")]
    [InlineData("grade")]
    [InlineData("classLetter")]
    [InlineData("phone")]
    [InlineData("parentPhone")]
    [InlineData("email")]
    public void Create_CustomFieldCodeCollidesWithCoreFieldName_Throws(string reservedCode)
    {
        var customFields = new List<RegistrationCustomField>
        {
            new(reservedCode, QuestionType.ShortText, "Test", null, RegistrationFieldRequirement.Optional, 200, null, null, 9),
        };

        var act = () => RegistrationFormDefinition.Create(ValidCoreFields(), customFields);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("REGISTRATION_FORM_FIELD_CODE_DUPLICATE");
    }

    [Theory]
    [InlineData(QuestionType.SingleChoice)]
    [InlineData(QuestionType.MultiChoice)]
    public void Create_ChoiceFieldWithFewerThanTwoOptions_Throws(QuestionType type)
    {
        var options = new List<RegistrationCustomFieldOption> { new("Bitta variant", "1", 1) };
        var customFields = new List<RegistrationCustomField>
        {
            new("CHOICE", type, "Tanlov", null, RegistrationFieldRequirement.Optional, null, null, options, 9),
        };

        var act = () => RegistrationFormDefinition.Create(ValidCoreFields(), customFields);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("REGISTRATION_FORM_CHOICE_OPTIONS_INSUFFICIENT");
    }

    [Fact]
    public void Create_ChoiceFieldWithNullOptions_Throws()
    {
        var customFields = new List<RegistrationCustomField>
        {
            new("CHOICE", QuestionType.SingleChoice, "Tanlov", null, RegistrationFieldRequirement.Optional, null, null, null, 9),
        };

        var act = () => RegistrationFormDefinition.Create(ValidCoreFields(), customFields);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("REGISTRATION_FORM_CHOICE_OPTIONS_INSUFFICIENT");
    }

    [Fact]
    public void Create_DuplicateOptionValue_Throws()
    {
        var options = new List<RegistrationCustomFieldOption> { new("Ha", "1", 1), new("Yo'q", "1", 2) };
        var customFields = new List<RegistrationCustomField>
        {
            new("CHOICE", QuestionType.SingleChoice, "Tanlov", null, RegistrationFieldRequirement.Optional, null, null, options, 9),
        };

        var act = () => RegistrationFormDefinition.Create(ValidCoreFields(), customFields);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("REGISTRATION_FORM_OPTION_VALUE_DUPLICATE");
    }

    [Theory]
    [InlineData(QuestionType.Likert5)]
    [InlineData(QuestionType.Likert7)]
    [InlineData(QuestionType.Binary)]
    [InlineData(QuestionType.ForcedChoice)]
    public void Create_DisallowedFieldType_ThrowsArgumentException(QuestionType type)
    {
        var customFields = new List<RegistrationCustomField>
        {
            new("BAD_TYPE", type, "Test", null, RegistrationFieldRequirement.Optional, null, null, null, 9),
        };

        var act = () => RegistrationFormDefinition.Create(ValidCoreFields(), customFields);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Json_RoundTrips_WithCamelCaseAndStringEnums()
    {
        var options = new List<RegistrationCustomFieldOption> { new("Ha", "yes", 1), new("Yo'q", "no", 2) };
        var customFields = new List<RegistrationCustomField>
        {
            new("PARENT_JOB", QuestionType.ShortText, "Ota-onangiz kasbi", "Masalan: o'qituvchi", RegistrationFieldRequirement.Optional, 200, null, null, 9),
            new("HAS_SIBLING", QuestionType.SingleChoice, "Aka-uka/opa-singil bormi?", null, RegistrationFieldRequirement.Required, null, null, options, 10),
        };
        var definition = RegistrationFormDefinition.Create(ValidCoreFields(), customFields);

        var json = RegistrationFormDefinitionJson.Serialize(definition);

        json.Should().Contain("\"coreFields\"");
        json.Should().Contain("\"fullName\":{\"requirement\":\"Required\"");
        json.Should().Contain("\"customFields\"");
        json.Should().Contain("\"type\":\"ShortText\"");
        json.Should().Contain("\"type\":\"SingleChoice\"");

        var roundTripped = RegistrationFormDefinitionJson.Deserialize(json);

        // `.Be()` EMAS — `CustomFields` (`IReadOnlyList&lt;T&gt;`) record'ning generatsiya
        // qilingan tenglik solishtiruvi UCHUN mos emas (`List&lt;T&gt;` strukturaviy emas,
        // havola bo'yicha solishtiradi), shu sabab chuqur (structural) solishtiruv kerak.
        roundTripped.Should().BeEquivalentTo(definition);
    }

    [Fact]
    public void Json_Deserialize_NullOrEmpty_ReturnsNull()
    {
        RegistrationFormDefinitionJson.Deserialize(null).Should().BeNull();
        RegistrationFormDefinitionJson.Deserialize(string.Empty).Should().BeNull();
    }
}
