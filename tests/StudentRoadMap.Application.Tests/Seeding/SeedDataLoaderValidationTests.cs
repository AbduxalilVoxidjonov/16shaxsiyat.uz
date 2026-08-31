using FluentAssertions;
using StudentRoadMap.Application.Seeding;

namespace StudentRoadMap.Application.Tests.Seeding;

/// <summary>
/// `SeedDataLoader.ParseTestDefinition` — buzilgan/majburiy maydoni yo'q JSON'lar uchun
/// aniq xato berishini tekshiradi (`prompts/04-katalog-va-seed-infratuzilma.md`: "noto'g'ri
/// JSON aniq xato beradi").
/// </summary>
public sealed class SeedDataLoaderValidationTests
{
    private const string ValidQuestion = """
        { "code": "Q1", "order": 1, "textUz": "Savol matni", "type": "Likert5", "scale": "EI", "direction": 1, "weight": 1.0, "isRequired": true }
        """;

    [Fact]
    public void ParseTestDefinition_EmptyString_ThrowsSeedDataFormatException()
    {
        var act = () => SeedDataLoader.ParseTestDefinition(string.Empty, "empty.json");

        act.Should().Throw<SeedDataFormatException>().Which.SourceName.Should().Be("empty.json");
    }

    [Fact]
    public void ParseTestDefinition_BrokenJsonSyntax_ThrowsSeedDataFormatException()
    {
        const string broken = "{ \"code\": \"BIG5\", \"questions\": [ ";

        var act = () => SeedDataLoader.ParseTestDefinition(broken, "broken.json");

        act.Should().Throw<SeedDataFormatException>();
    }

    [Fact]
    public void ParseTestDefinition_MissingCode_ThrowsSeedDataFormatException()
    {
        var json = $$"""
            { "nameUz": "Test", "displayOrder": 1, "estimatedMinutes": 5, "pageSize": 10, "questions": [ {{ValidQuestion}} ] }
            """;

        var act = () => SeedDataLoader.ParseTestDefinition(json, "no-code.json");

        act.Should().Throw<SeedDataFormatException>().WithMessage("*code*");
    }

    [Fact]
    public void ParseTestDefinition_EmptyQuestionsArray_ThrowsSeedDataFormatException()
    {
        var json = """
            { "code": "BIG5", "nameUz": "Test", "displayOrder": 1, "estimatedMinutes": 5, "pageSize": 10, "questions": [] }
            """;

        var act = () => SeedDataLoader.ParseTestDefinition(json, "no-questions.json");

        act.Should().Throw<SeedDataFormatException>().WithMessage("*savollar*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-3)]
    public void ParseTestDefinition_InvalidDirection_ThrowsSeedDataFormatException(int direction)
    {
        var json = $$"""
            {
              "code": "BIG5", "nameUz": "Test", "displayOrder": 1, "estimatedMinutes": 5, "pageSize": 10,
              "questions": [
                { "code": "Q1", "order": 1, "textUz": "Matn", "type": "Likert5", "scale": "O", "direction": {{direction}}, "weight": 1.0, "isRequired": true }
              ]
            }
            """;

        var act = () => SeedDataLoader.ParseTestDefinition(json, "bad-direction.json");

        act.Should().Throw<SeedDataFormatException>().WithMessage("*direction*");
    }

    [Fact]
    public void ParseTestDefinition_UnknownQuestionType_ThrowsSeedDataFormatException()
    {
        var json = """
            {
              "code": "BIG5", "nameUz": "Test", "displayOrder": 1, "estimatedMinutes": 5, "pageSize": 10,
              "questions": [
                { "code": "Q1", "order": 1, "textUz": "Matn", "type": "NotARealType", "scale": "O", "direction": 1, "weight": 1.0, "isRequired": true }
              ]
            }
            """;

        var act = () => SeedDataLoader.ParseTestDefinition(json, "bad-type.json");

        act.Should().Throw<SeedDataFormatException>().WithMessage("*type*");
    }

    [Fact]
    public void ParseTestDefinition_DuplicateQuestionCode_ThrowsSeedDataFormatException()
    {
        var json = $$"""
            {
              "code": "BIG5", "nameUz": "Test", "displayOrder": 1, "estimatedMinutes": 5, "pageSize": 10,
              "questions": [ {{ValidQuestion}}, {{ValidQuestion}} ]
            }
            """;

        var act = () => SeedDataLoader.ParseTestDefinition(json, "dup.json");

        act.Should().Throw<SeedDataFormatException>().WithMessage("*takrorlangan*");
    }

    [Fact]
    public void ParseTestDefinition_MissingQuestionScale_ThrowsSeedDataFormatException()
    {
        var json = """
            {
              "code": "BIG5", "nameUz": "Test", "displayOrder": 1, "estimatedMinutes": 5, "pageSize": 10,
              "questions": [
                { "code": "Q1", "order": 1, "textUz": "Matn", "type": "Likert5", "scale": "", "direction": 1, "weight": 1.0, "isRequired": true }
              ]
            }
            """;

        var act = () => SeedDataLoader.ParseTestDefinition(json, "no-scale.json");

        act.Should().Throw<SeedDataFormatException>().WithMessage("*scale*");
    }

    [Fact]
    public void ParseTestDefinition_ValidMinimalJson_ParsesSuccessfully()
    {
        var json = $$"""
            {
              "code": "BIG5", "nameUz": "Test", "displayOrder": 1, "estimatedMinutes": 5, "pageSize": 10,
              "questions": [ {{ValidQuestion}} ]
            }
            """;

        var dto = SeedDataLoader.ParseTestDefinition(json, "ok.json");

        dto.Code.Should().Be("BIG5");
        dto.Questions.Should().ContainSingle();
    }
}
