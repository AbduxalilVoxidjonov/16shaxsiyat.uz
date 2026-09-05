using FluentAssertions;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Jobs;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Domain.Tests;

/// <summary>
/// Har bir enum qiymati `docs/05-database-schema.md` 3-bo'limidagi raqamlarga aynan mos kelishini
/// tekshiradi. Bu qiymatlar DB'ga yoziladi — bitta xato ham qimmatga tushadi.
/// </summary>
public sealed class EnumValueTests
{
    [Theory]
    [InlineData(AssessmentStatus.Draft, 0)]
    [InlineData(AssessmentStatus.InProgress, 1)]
    [InlineData(AssessmentStatus.Completed, 2)]
    [InlineData(AssessmentStatus.Analyzing, 3)]
    [InlineData(AssessmentStatus.Analyzed, 4)]
    [InlineData(AssessmentStatus.AnalysisFailed, 5)]
    [InlineData(AssessmentStatus.Abandoned, 6)]
    public void AssessmentStatus_MatchesDatabaseSchema(AssessmentStatus value, int expected) =>
        ((int)value).Should().Be(expected);

    [Theory]
    [InlineData(TestStatus.NotStarted, 0)]
    [InlineData(TestStatus.InProgress, 1)]
    [InlineData(TestStatus.Completed, 2)]
    public void TestStatus_MatchesDatabaseSchema(TestStatus value, int expected) =>
        ((int)value).Should().Be(expected);

    [Theory]
    [InlineData(SchoolKind.School, 1)]
    [InlineData(SchoolKind.PublicSpace, 2)]
    public void SchoolKind_MatchesDatabaseSchema(SchoolKind value, int expected) =>
        ((int)value).Should().Be(expected);

    [Theory]
    [InlineData(Gender.Unspecified, 0)]
    [InlineData(Gender.Male, 1)]
    [InlineData(Gender.Female, 2)]
    public void Gender_MatchesDatabaseSchema(Gender value, int expected) =>
        ((int)value).Should().Be(expected);

    [Theory]
    [InlineData(QuestionType.Likert5, 1)]
    [InlineData(QuestionType.Likert7, 2)]
    [InlineData(QuestionType.Binary, 3)]
    [InlineData(QuestionType.SingleChoice, 4)]
    [InlineData(QuestionType.ForcedChoice, 5)]
    public void QuestionType_MatchesDatabaseSchema(QuestionType value, int expected) =>
        ((int)value).Should().Be(expected);

    [Theory]
    [InlineData(TestKind.Standard, 1)]
    [InlineData(TestKind.Custom, 2)]
    public void TestKind_MatchesDatabaseSchema(TestKind value, int expected) =>
        ((int)value).Should().Be(expected);

    [Theory]
    [InlineData(TestDefinitionStatus.Draft, 1)]
    [InlineData(TestDefinitionStatus.Published, 2)]
    [InlineData(TestDefinitionStatus.Archived, 3)]
    public void TestDefinitionStatus_MatchesDatabaseSchema(TestDefinitionStatus value, int expected) =>
        ((int)value).Should().Be(expected);

    [Theory]
    [InlineData(ReliabilityFlag.Reliable, 1)]
    [InlineData(ReliabilityFlag.Questionable, 2)]
    [InlineData(ReliabilityFlag.Unreliable, 3)]
    public void ReliabilityFlag_MatchesDatabaseSchema(ReliabilityFlag value, int expected) =>
        ((int)value).Should().Be(expected);

    [Theory]
    [InlineData(ActivityLevel.Passive, 1)]
    [InlineData(ActivityLevel.LowActive, 2)]
    [InlineData(ActivityLevel.Moderate, 3)]
    [InlineData(ActivityLevel.Active, 4)]
    [InlineData(ActivityLevel.HighlyActive, 5)]
    public void ActivityLevel_MatchesDatabaseSchema(ActivityLevel value, int expected) =>
        ((int)value).Should().Be(expected);

    [Theory]
    [InlineData(AiProvider.Gemini, 1)]
    [InlineData(AiProvider.OpenAi, 2)]
    [InlineData(AiProvider.Anthropic, 3)]
    public void AiProvider_MatchesDatabaseSchema(AiProvider value, int expected) =>
        ((int)value).Should().Be(expected);

    [Theory]
    [InlineData(AiAnalysisStatus.Pending, 0)]
    [InlineData(AiAnalysisStatus.Running, 1)]
    [InlineData(AiAnalysisStatus.Succeeded, 2)]
    [InlineData(AiAnalysisStatus.Failed, 3)]
    public void AiAnalysisStatus_MatchesDatabaseSchema(AiAnalysisStatus value, int expected) =>
        ((int)value).Should().Be(expected);

    [Theory]
    [InlineData(AdminRole.SuperAdmin, 1)]
    [InlineData(AdminRole.SchoolAdmin, 2)]
    [InlineData(AdminRole.Psychologist, 3)]
    public void AdminRole_MatchesDatabaseSchema(AdminRole value, int expected) =>
        ((int)value).Should().Be(expected);

    /// <summary>`analysis_jobs.status` — P18 (`prompts/18`), `AnalysisJobConfiguration`.</summary>
    [Theory]
    [InlineData(AnalysisJobStatus.Pending, 0)]
    [InlineData(AnalysisJobStatus.Running, 1)]
    [InlineData(AnalysisJobStatus.Completed, 2)]
    [InlineData(AnalysisJobStatus.Failed, 3)]
    public void AnalysisJobStatus_MatchesDatabaseSchema(AnalysisJobStatus value, int expected) =>
        ((int)value).Should().Be(expected);
}
