using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using StudentRoadMap.Domain.Catalog.Branching;

namespace StudentRoadMap.Domain.Tests.Branching;

/// <summary>
/// `tests/fixtures/visibility-golden.json` — oltin fikstura (`docs/18` §6.1). Bu faylni C# VA
/// TS tomoni ikkalasi ham o'qiydi — C#/TS `VisibleQuestionResolver` xatti-harakati bir-biridan
/// ajralib ketmasligining yagona kafolati. Fikstura O'ZGARTIRILMAYDI — mos kelmasa KOD tuzatiladi.
/// </summary>
public sealed class VisibilityGoldenTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static IEnumerable<object[]> Cases()
    {
        var fixture = LoadFixture();
        return fixture.Cases.Select(c => new object[] { c });
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void GoldenCase_ProducesExpectedVisibility(FixtureCase testCase)
    {
        var sectionIds = testCase.Sections.ToDictionary(s => s.Code, _ => Guid.NewGuid(), StringComparer.Ordinal);
        var questionIds = testCase.Questions.ToDictionary(q => q.Code, _ => Guid.NewGuid(), StringComparer.Ordinal);

        var sections = testCase.Sections
            .Select(s => new SectionSnapshot(sectionIds[s.Code], s.Code, s.Order, s.Visibility))
            .ToList();

        var questions = testCase.Questions
            .Select(q => new QuestionSnapshot(
                questionIds[q.Code],
                q.Code,
                q.Order,
                IsActive: true,
                SectionId: q.SectionCode is null ? null : sectionIds[q.SectionCode],
                q.Visibility))
            .ToList();

        var answers = (testCase.Answers ?? new Dictionary<string, FixtureAnswer>(StringComparer.Ordinal))
            .ToDictionary(
                kv => kv.Key,
                kv => new AnswerSnapshot(kv.Value.RawValue, kv.Value.TextValue, kv.Value.SelectedValues ?? []),
                StringComparer.Ordinal);

        var map = VisibleQuestionResolver.Resolve(sections, questions, answers);

        var visibleSectionCodes = sections.Where(s => map.VisibleSectionIds.Contains(s.Id)).Select(s => s.Code);
        var visibleQuestionCodes = questions.Where(q => map.VisibleQuestionIds.Contains(q.Id)).Select(q => q.Code);

        visibleSectionCodes.Should().BeEquivalentTo(testCase.ExpectedVisibleSectionCodes, $"case '{testCase.Name}' — bo'limlar");
        visibleQuestionCodes.Should().BeEquivalentTo(testCase.ExpectedVisibleQuestionCodes, $"case '{testCase.Name}' — savollar");
    }

    private static FixtureRoot LoadFixture()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures", "visibility-golden.json");
        var json = File.ReadAllText(path);
        var root = JsonSerializer.Deserialize<FixtureRoot>(json, JsonOptions);
        return root ?? throw new InvalidOperationException("Oltin fikstura o'qilmadi: 'visibility-golden.json'.");
    }

    public sealed record FixtureRoot(List<FixtureCase> Cases);

    public sealed record FixtureCase(
        string Name,
        List<FixtureSection> Sections,
        List<FixtureQuestion> Questions,
        Dictionary<string, FixtureAnswer>? Answers,
        List<string> ExpectedVisibleSectionCodes,
        List<string> ExpectedVisibleQuestionCodes)
    {
        /// <summary>xUnit `[Theory]` parametrlari test nomida ko'rinishi uchun.</summary>
        public override string ToString() => Name;
    }

    public sealed record FixtureSection(string Code, int Order, VisibilityRule? Visibility);

    public sealed record FixtureQuestion(string Code, int Order, string Type, string? SectionCode, VisibilityRule? Visibility, List<int>? Options);

    public sealed record FixtureAnswer(int? RawValue, string? TextValue, List<int>? SelectedValues);
}
