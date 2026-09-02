using FluentAssertions;
using StudentRoadMap.Domain.Assessments;

namespace StudentRoadMap.Domain.Tests.Assessments;

/// <summary>`TestResult.ReplaceScores` — `prompts/15` `RecalculateScores` uchun qo'shildi.</summary>
public sealed class TestResultTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static TestResult CreateSample() => TestResult.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "MBTI16",
        rawScoresJson: """{"EI":10}""",
        normalizedScoresJson: """{"EI":40.0}""",
        scoringVersion: 1,
        testVersion: 1,
        computedAt: Now,
        resultCode: "ISTJ",
        levelsJson: """{"EI":"I"}""",
        compositeIndex: null,
        flagsJson: "[]");

    [Fact]
    public void ReplaceScores_HammaMaydonniAlmashtiradi_TestVersionOzgarmaydi()
    {
        var testResult = CreateSample();
        var recomputedAt = Now.AddDays(1);

        testResult.ReplaceScores(
            resultCode: "INTJ",
            rawScoresJson: """{"EI":12}""",
            normalizedScoresJson: """{"EI":45.0}""",
            levelsJson: """{"EI":"I"}""",
            compositeIndex: 68.4,
            flagsJson: """["Borderline:EI"]""",
            scoringVersion: 2,
            computedAt: recomputedAt);

        testResult.ResultCode.Should().Be("INTJ");
        testResult.RawScoresJson.Should().Be("""{"EI":12}""");
        testResult.NormalizedScoresJson.Should().Be("""{"EI":45.0}""");
        testResult.CompositeIndex.Should().Be(68.4);
        testResult.FlagsJson.Should().Be("""["Borderline:EI"]""");
        testResult.ScoringVersion.Should().Be(2);
        testResult.ComputedAt.Should().Be(recomputedAt);

        // BR-9: `TestVersion` — o'quvchi haqiqatda topshirgan anketa shakli, formula qayta
        // hisoblanganda o'zgarmaydi.
        testResult.TestVersion.Should().Be(1);
    }

    [Fact]
    public void ReplaceScores_IkkiMartaBirXilKirish_BirXilNatija()
    {
        // Idempotentlik (`prompts/15` MAXSUS DIQQAT #6): xuddi shu kirish bilan ikki marta
        // chaqirilsa — natija bir xil bo'lishi kerak (sof funksiya, yon ta'sirsiz).
        var first = CreateSample();
        var second = CreateSample();

        first.ReplaceScores("INTJ", """{"EI":12}""", """{"EI":45.0}""", """{"EI":"I"}""", 68.4, "[]", 2, Now.AddDays(1));
        second.ReplaceScores("INTJ", """{"EI":12}""", """{"EI":45.0}""", """{"EI":"I"}""", 68.4, "[]", 2, Now.AddDays(1));

        second.ResultCode.Should().Be(first.ResultCode);
        second.RawScoresJson.Should().Be(first.RawScoresJson);
        second.NormalizedScoresJson.Should().Be(first.NormalizedScoresJson);
        second.CompositeIndex.Should().Be(first.CompositeIndex);
        second.FlagsJson.Should().Be(first.FlagsJson);
        second.ScoringVersion.Should().Be(first.ScoringVersion);
    }
}
