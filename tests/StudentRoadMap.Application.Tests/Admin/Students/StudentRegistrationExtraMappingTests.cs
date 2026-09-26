using FluentAssertions;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Settings;

namespace StudentRoadMap.Application.Tests.Admin.Students;

/// <summary>
/// `StudentRegistrationExtraMapping` — `ProfileExtra` jsonb → admin profilidagi `yorliq — qiymat`
/// ro'yxati. Tanlov javoblari `Order` sifatida saqlanadi, shu sabab variant matniga qaytarilishi
/// va sozlamadan o'chirilgan maydon javobi yo'qolmasligi tekshiriladi.
/// </summary>
public sealed class StudentRegistrationExtraMappingTests
{
    private static readonly IReadOnlyList<RegistrationCustomField> Fields =
    [
        new("TRANSPORT", QuestionType.MultiChoice, "Transport", null, RegistrationFieldRequirement.Optional, null, null,
            [new("Avtobus", "bus", 1), new("Metro", "metro", 2), new("Piyoda", "walk", 3)], Order: 2),
        new("PARENT_JOB", QuestionType.ShortText, "Ota-ona kasbi", null, RegistrationFieldRequirement.Optional, null, null, null, Order: 1),
        new("SHIFT", QuestionType.SingleChoice, "Smena", null, RegistrationFieldRequirement.Optional, null, null,
            [new("1-smena", "1", 1), new("2-smena", "2", 2)], Order: 3),
    ];

    [Fact]
    public void Map_NullYokiBoshJson_BoshRoyxat()
    {
        StudentRegistrationExtraMapping.Map(null, Fields).Should().BeEmpty();
        StudentRegistrationExtraMapping.Map("  ", Fields).Should().BeEmpty();
    }

    [Fact]
    public void Map_BuzuqJson_YiqilmaydiBoshQaytaradi()
    {
        StudentRegistrationExtraMapping.Map("{not json", Fields).Should().BeEmpty();
        StudentRegistrationExtraMapping.Map("[1,2]", Fields).Should().BeEmpty();
    }

    [Fact]
    public void Map_TanlovlarVariantMatnigaOgiriladi_SozlamaTartibida()
    {
        const string json = """{"TRANSPORT":[1,3],"SHIFT":2,"PARENT_JOB":"O'qituvchi"}""";

        var result = StudentRegistrationExtraMapping.Map(json, Fields);

        result.Should().Equal(
            new AdminStudentExtraFieldDto("PARENT_JOB", "Ota-ona kasbi", "O'qituvchi"),
            new AdminStudentExtraFieldDto("TRANSPORT", "Transport", "Avtobus, Piyoda"),
            new AdminStudentExtraFieldDto("SHIFT", "Smena", "2-smena"));
    }

    [Fact]
    public void Map_SozlamadanOchirilganMaydon_KodBilanOxiridaKorsatiladi()
    {
        const string json = """{"OLD_FIELD":"eski javob","PARENT_JOB":"Shifokor"}""";

        var result = StudentRegistrationExtraMapping.Map(json, Fields);

        result.Should().Equal(
            new AdminStudentExtraFieldDto("PARENT_JOB", "Ota-ona kasbi", "Shifokor"),
            new AdminStudentExtraFieldDto("OLD_FIELD", "OLD_FIELD", "eski javob"));
    }

    [Fact]
    public void Map_NomalumVariantRaqami_XomQiymatQoladi()
    {
        var result = StudentRegistrationExtraMapping.Map("""{"SHIFT":9}""", Fields);

        result.Should().ContainSingle().Which.Value.Should().Be("9");
    }
}
