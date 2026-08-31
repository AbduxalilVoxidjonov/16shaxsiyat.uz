using FluentAssertions;
using StudentRoadMap.Application.Seeding;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Tests.Seeding;

/// <summary>
/// `SeedDataLoader`ni **haqiqiy** 4 ta seed JSON fayli bilan sinaydi (`mbti16.json`, `big5.json`,
/// `riasec.json`, `activity.json` — manba `Infrastructure/Persistence/SeedData/test-definitions/`,
/// chiqish katalogiga csproj orqali nusxalanadi). Bu fayllar QA'dan o'tgan va **o'zgartirilmaydi**
/// (topshiriq shartiga ko'ra) — testlar faqat ularni o'qiydi.
/// </summary>
public sealed class SeedDataLoaderRealFixturesTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static string SeedDirectory =>
        Path.Combine(AppContext.BaseDirectory, "SeedData", "test-definitions");

    private static IReadOnlyList<string> AllFixtureFiles() =>
        Directory.EnumerateFiles(SeedDirectory, "*.json").OrderBy(f => f, StringComparer.Ordinal).ToList();

    [Fact]
    public void SeedDirectory_ContainsExactlyFourTestDefinitionFiles()
    {
        AllFixtureFiles().Should().HaveCount(4, "test bankida 4 ta metodika bor (MBTI16, BIG5, RIASEC, ACTIVITY)");
    }

    [Fact]
    public void ParseTestDefinition_AllFourFixtures_TotalQuestionCountIs190()
    {
        var totalQuestions = AllFixtureFiles()
            .Select(file => SeedDataLoader.ParseTestDefinition(File.ReadAllText(file), Path.GetFileName(file)))
            .Sum(dto => dto.Questions.Count);

        totalQuestions.Should().Be(190, "docs/03-psixologik-metodikalar.md 9-bo'lim: 60+50+48+32=190");
    }

    [Theory]
    [InlineData("mbti16.json", "MBTI16", 60)]
    [InlineData("big5.json", "BIG5", 50)]
    [InlineData("riasec.json", "RIASEC", 48)]
    [InlineData("activity.json", "ACTIVITY", 32)]
    public void ParseTestDefinition_EachFixture_HasExpectedCodeAndQuestionCount(string fileName, string expectedCode, int expectedCount)
    {
        var path = Path.Combine(SeedDirectory, fileName);
        var dto = SeedDataLoader.ParseTestDefinition(File.ReadAllText(path), fileName);

        dto.Code.Should().Be(expectedCode);
        dto.Questions.Should().HaveCount(expectedCount);
        dto.Questions.Should().OnlyHaveUniqueItems(q => q.Code);
    }

    [Theory]
    [InlineData("mbti16.json")]
    [InlineData("big5.json")]
    [InlineData("riasec.json")]
    [InlineData("activity.json")]
    public void ParseTestDefinition_EachQuestion_HasValidScaleDirectionAndType(string fileName)
    {
        var path = Path.Combine(SeedDirectory, fileName);
        var dto = SeedDataLoader.ParseTestDefinition(File.ReadAllText(path), fileName);

        foreach (var question in dto.Questions)
        {
            question.Code.Should().NotBeNullOrWhiteSpace();
            question.Scale.Should().NotBeNullOrWhiteSpace();
            question.Direction.Should().BeOneOf(1, -1);
            question.Weight.Should().BeGreaterThan(0);
            Enum.TryParse<QuestionType>(question.Type, out _).Should().BeTrue($"'{question.Code}' turi '{question.Type}' — QuestionType enumiga mos kelishi kerak");
        }
    }

    [Fact]
    public void ParseTestDefinition_Mbti16_FirstQuestionMatchesKnownFields()
    {
        var path = Path.Combine(SeedDirectory, "mbti16.json");
        var dto = SeedDataLoader.ParseTestDefinition(File.ReadAllText(path), "mbti16.json");

        var first = dto.Questions.Single(q => q.Code == "MB-Q01");
        first.Scale.Should().Be("EI");
        first.Direction.Should().Be(1);
        first.Type.Should().Be("Likert5");
    }

    [Fact]
    public void ToDomainSystemTestDefinition_Mbti16_BuildsPublishedSystemAggregate()
    {
        var path = Path.Combine(SeedDirectory, "mbti16.json");
        var dto = SeedDataLoader.ParseTestDefinition(File.ReadAllText(path), "mbti16.json");
        var testDefinitionId = Guid.NewGuid();

        var testDefinition = SeedDataLoader.ToDomainSystemTestDefinition(dto, testDefinitionId, _ => Guid.NewGuid(), Now);

        testDefinition.Code.Should().Be("MBTI16");
        testDefinition.IsSystem.Should().BeTrue();
        testDefinition.Kind.Should().Be(TestKind.Standard);
        testDefinition.Status.Should().Be(TestDefinitionStatus.Published);
        testDefinition.ScoringStrategyCode.Should().Be("MBTI16");
        testDefinition.QuestionCount.Should().Be(60);
        testDefinition.Questions.Should().OnlyContain(q => q.IsSystem && q.TestDefinitionId == testDefinitionId);
    }

    [Theory]
    [InlineData("mbti16.json", "EI", 15)]
    [InlineData("mbti16.json", "SN", 15)]
    [InlineData("mbti16.json", "TF", 15)]
    [InlineData("mbti16.json", "JP", 15)]
    [InlineData("big5.json", "O", 10)]
    [InlineData("big5.json", "N", 10)]
    [InlineData("riasec.json", "R", 8)]
    [InlineData("riasec.json", "CONV", 8)]
    [InlineData("activity.json", "MOT", 8)]
    [InlineData("activity.json", "ENG", 8)]
    public void ParseTestDefinition_ScaleGrouping_MatchesExpectedQuestionCount(string fileName, string scale, int expectedCount)
    {
        var path = Path.Combine(SeedDirectory, fileName);
        var dto = SeedDataLoader.ParseTestDefinition(File.ReadAllText(path), fileName);

        dto.Questions.Count(q => q.Scale == scale).Should().Be(expectedCount);
    }
}
