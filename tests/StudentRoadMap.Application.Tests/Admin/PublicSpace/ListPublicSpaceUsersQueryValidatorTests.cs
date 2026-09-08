using FluentAssertions;
using StudentRoadMap.Application.Admin.PublicSpace.ListUsers;

namespace StudentRoadMap.Application.Tests.Admin.PublicSpace;

public sealed class ListPublicSpaceUsersQueryValidatorTests
{
    private readonly ListPublicSpaceUsersQueryValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("all")]
    [InlineData("never_started")]
    [InlineData("IN_PROGRESS")]
    [InlineData("completed")]
    [InlineData("deleted")]
    public void Status_RuxsatEtilganQiymatlar_OTadi(string? status)
    {
        var result = _validator.Validate(new ListPublicSpaceUsersQuery(null, status, 1, 20, null));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("abandoned")]
    [InlineData("hammasi")]
    public void Status_NomaLumQiymat_RadEtiladi(string status)
    {
        var result = _validator.Validate(new ListPublicSpaceUsersQuery(null, status, 1, 20, null));

        result.IsValid.Should().BeFalse("noma'lum holat jimgina 'hammasi'ga tushmasin — `all` uchun aniq qiymat bor");
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(ListPublicSpaceUsersQuery.Status));
    }

    [Fact]
    public void Search_JudaUzun_RadEtiladi()
    {
        var result = _validator.Validate(new ListPublicSpaceUsersQuery(new string('a', 201), null, 1, 20, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void PublicUserStatusFilter_Parse_BoShAllNomaLumNull()
    {
        PublicUserStatusFilter.Parse(null).Should().Be(PublicUserStatusFilter.All);
        PublicUserStatusFilter.Parse("  ").Should().Be(PublicUserStatusFilter.All);
        PublicUserStatusFilter.Parse("Completed").Should().Be(PublicUserStatusFilter.Completed);
        PublicUserStatusFilter.Parse("xyz").Should().BeNull();
    }
}
