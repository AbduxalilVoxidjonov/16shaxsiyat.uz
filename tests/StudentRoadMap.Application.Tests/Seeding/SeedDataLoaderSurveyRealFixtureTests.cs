using FluentAssertions;
using StudentRoadMap.Application.Seeding;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Catalog.Branching;

namespace StudentRoadMap.Application.Tests.Seeding;

/// <summary>
/// `SeedDataLoader.ParseSurvey`/`ToDomainCustomDraftSurvey`ni **haqiqiy** namunaviy so'rovnoma
/// fayli bilan sinaydi (`Infrastructure/Persistence/SeedData/surveys/intellect-survey.json`,
/// `docs/18` §7). Bu — `DbSeeder.SeedSurveysAsync` ishlatadigan AYNAN o'sha yo'l, shu sabab bu
/// yerdagi tekshiruvlar production seed natijasini to'g'ridan-to'g'ri qamraydi. Fayl QA'dan
/// o'tgan va **o'zgartirilmaydi** (topshiriq sharti) — testlar faqat uni o'qiydi.
/// </summary>
public sealed class SeedDataLoaderSurveyRealFixtureTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static string FixturePath =>
        Path.Combine(AppContext.BaseDirectory, "SeedData", "surveys", "intellect-survey.json");

    private static SurveySeedDto ParseFixture() =>
        SeedDataLoader.ParseSurvey(File.ReadAllText(FixturePath), "intellect-survey.json");

    [Fact]
    public void ParseSurvey_IntellectSurvey_HasExpectedShape()
    {
        var dto = ParseFixture();

        dto.Code.Should().Be("INTELLECT-SURVEY");
        dto.ScoringMode.Should().Be("Survey");
        dto.Sections.Should().HaveCount(5);
        dto.Questions.Should().HaveCount(25);
        dto.Questions.Should().OnlyHaveUniqueItems(q => q.Code);
        dto.Sections.Should().OnlyHaveUniqueItems(s => s.Code);
    }

    [Fact]
    public void ParseSurvey_Q1_6_FilterQuestionHasThreeOptionsAndNoVisibility()
    {
        var dto = ParseFixture();

        var q16 = dto.Questions.Single(q => q.Code == "Q1_6");
        q16.SectionCode.Should().Be("S1");
        q16.Type.Should().Be("SingleChoice");
        q16.Options.Should().HaveCount(3);
        q16.Visibility.Should().BeNull("Q1_6 — filtr savoli, o'zi shartsiz");
    }

    [Fact]
    public void ParseSurvey_SectionsS2AS2BS2C_HaveEqualsVisibilityOnQ1_6()
    {
        var dto = ParseFixture();

        var s2A = dto.Sections.Single(s => s.Code == "S2A");
        var s2B = dto.Sections.Single(s => s.Code == "S2B");
        var s2C = dto.Sections.Single(s => s.Code == "S2C");

        s2A.Visibility.Should().BeEquivalentTo(new VisibilityRule(VisibilityMatch.All, [new VisibilityCondition("Q1_6", VisibilityOperator.Equals, [1])]));
        s2B.Visibility.Should().BeEquivalentTo(new VisibilityRule(VisibilityMatch.All, [new VisibilityCondition("Q1_6", VisibilityOperator.Equals, [2])]));
        s2C.Visibility.Should().BeEquivalentTo(new VisibilityRule(VisibilityMatch.All, [new VisibilityCondition("Q1_6", VisibilityOperator.Equals, [3])]));
    }

    [Theory]
    [InlineData("Q2A_1_OTHER", "Q2A_1")]
    [InlineData("Q2B_1_OTHER", "Q2B_1")]
    public void ParseSurvey_OtherQuestions_HaveContainsAnyOrEqualsVisibilityOnParentChoice(string otherCode, string parentCode)
    {
        var dto = ParseFixture();

        var other = dto.Questions.Single(q => q.Code == otherCode);
        other.Type.Should().Be("ShortText");
        other.Visibility.Should().NotBeNull();
        other.Visibility!.Conditions.Should().ContainSingle(c => c.QuestionCode == parentCode);
    }

    [Fact]
    public void ParseSurvey_EachQuestion_HasValidScaleDirectionAndType()
    {
        var dto = ParseFixture();

        foreach (var question in dto.Questions)
        {
            question.Code.Should().NotBeNullOrWhiteSpace();
            question.Scale.Should().Be("SURVEY");
            question.Direction.Should().Be(1);
            question.Weight.Should().BeGreaterThan(0);
            Enum.TryParse<QuestionType>(question.Type, out _).Should().BeTrue($"'{question.Code}' turi '{question.Type}' — QuestionType enumiga mos kelishi kerak");
        }
    }

    [Fact]
    public void ToDomainCustomDraftSurvey_IntellectSurvey_BuildsCustomDraftAggregate()
    {
        var dto = ParseFixture();
        var testDefinitionId = Guid.NewGuid();

        var test = SeedDataLoader.ToDomainCustomDraftSurvey(dto, testDefinitionId, _ => Guid.NewGuid(), _ => Guid.NewGuid(), Now);

        test.Code.Should().Be("INTELLECT-SURVEY");
        test.Kind.Should().Be(TestKind.Custom);
        test.IsSystem.Should().BeFalse();
        test.ScoringMode.Should().Be(TestScoringMode.Survey);
        test.ScoringStrategyCode.Should().BeNull("Survey rejimida strategiya ishlatilmaydi");
        test.Status.Should().Be(TestDefinitionStatus.Draft, "namunaviy so'rovnoma HECH QACHON avtomatik nashr qilinmaydi (docs/18 §7)");
        test.Sections.Should().HaveCount(5);
        test.QuestionCount.Should().Be(25);
        test.Questions.Should().OnlyContain(q => !q.IsSystem && q.TestDefinitionId == testDefinitionId);
    }

    [Fact]
    public void ToDomainCustomDraftSurvey_QuestionsWithSectionCode_ResolveToCorrectSectionId()
    {
        var dto = ParseFixture();
        var test = SeedDataLoader.ToDomainCustomDraftSurvey(dto, Guid.NewGuid(), _ => Guid.NewGuid(), _ => Guid.NewGuid(), Now);

        var s2A = test.Sections.Single(s => s.Code == "S2A");
        var q2A1 = test.Questions.Single(q => q.Code == "Q2A_1");

        q2A1.SectionId.Should().Be(s2A.Id);
        q2A1.QuestionType.Should().Be(QuestionType.MultiChoice);
        q2A1.MinSelections.Should().Be(1);
        q2A1.Options.Should().HaveCount(8);
    }

    [Fact]
    public void ToDomainCustomDraftSurvey_TextAndPhoneQuestions_CarryPlaceholderAndPattern()
    {
        var dto = ParseFixture();
        var test = SeedDataLoader.ToDomainCustomDraftSurvey(dto, Guid.NewGuid(), _ => Guid.NewGuid(), _ => Guid.NewGuid(), Now);

        var phone = test.Questions.Single(q => q.Code == "Q1_3");
        phone.QuestionType.Should().Be(QuestionType.Phone);
        phone.InputPattern.Should().Be("^\\+?998[0-9]{9}$");
        phone.MaxLength.Should().Be(20);

        var longText = test.Questions.Single(q => q.Code == "Q2A_6");
        longText.QuestionType.Should().Be(QuestionType.LongText);
        longText.IsRequired.Should().BeFalse();
        longText.MaxLength.Should().Be(1000);
    }
}
