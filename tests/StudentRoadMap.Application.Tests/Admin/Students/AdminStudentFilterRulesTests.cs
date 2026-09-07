using FluentAssertions;
using StudentRoadMap.Application.Admin.Students.Export;
using StudentRoadMap.Application.Admin.Students.List;

namespace StudentRoadMap.Application.Tests.Admin.Students;

/// <summary>
/// `gender`/`ageMin`/`ageMax` validatsiya qoidalari (`AdminStudentFilterRules`) va ularni
/// ro'yxat/eksport validatorlari BIR XIL qo'llashi (`prompts/27` MAXSUS DIQQAT #1).
/// </summary>
public sealed class AdminStudentFilterRulesTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("Male", true)]
    [InlineData("female", true)]
    [InlineData(" Male ", true)]
    [InlineData("Unspecified", false)]
    [InlineData("maktab", false)]
    [InlineData("1", false)]
    public void IsValidGender_FaqatMaleVaFemale(string? gender, bool expected)
    {
        AdminStudentFilterRules.IsValidGender(gender).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(6, true)]
    [InlineData(99, true)]
    [InlineData(5, false)]
    [InlineData(100, false)]
    [InlineData(-1, false)]
    public void IsValidAge_6dan99gacha(int? age, bool expected)
    {
        AdminStudentFilterRules.IsValidAge(age).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData(11, null, true)]
    [InlineData(null, 14, true)]
    [InlineData(11, 14, true)]
    [InlineData(11, 11, true)]
    [InlineData(15, 11, false)]
    public void IsValidAgeOrder_MinMaxdanKattaBolmasin(int? ageMin, int? ageMax, bool expected)
    {
        AdminStudentFilterRules.IsValidAgeOrder(ageMin, ageMax).Should().Be(expected);
    }

    [Fact]
    public void ListVaExportValidatorlari_BirXilQoidalar()
    {
        var listValidator = new ListStudentsQueryValidator();
        var exportValidator = new ExportStudentsQueryValidator();

        var badList = new ListStudentsQuery(null, null, null, null, null, null, "Unspecified", 15, 11, null, null, null, 1, 20, null);
        var badExport = new ExportStudentsQuery(null, null, null, null, null, null, "Unspecified", 15, 11, null, null, null, null, null, null);

        var listErrors = listValidator.Validate(badList).Errors.Select(e => e.ErrorMessage).ToList();
        var exportErrors = exportValidator.Validate(badExport).Errors.Select(e => e.ErrorMessage).ToList();

        listErrors.Should().BeEquivalentTo(exportErrors);
        listErrors.Should().Contain(AdminStudentFilterRules.GenderMessage);
        listErrors.Should().Contain(AdminStudentFilterRules.AgeOrderMessage);

        var goodList = badList with { Gender = "Female", AgeMin = 11, AgeMax = 14 };
        listValidator.Validate(goodList).IsValid.Should().BeTrue();
    }
}
