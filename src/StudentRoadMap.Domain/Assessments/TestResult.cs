using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Assessments;

/// <summary>
/// Bitta test blokining hisoblangan natijasi. Ball maydonlari `jsonb` sifatida saqlanadi —
/// Domen qatlamida xom JSON matni sifatida ushlanadi, tuzilishini `Scoring` strategiyalari biladi
/// (`docs/04` 2.6-bo'lim).
/// </summary>
public sealed class TestResult : Entity
{
    public Guid AssessmentTestId { get; private set; }

    public Guid AssessmentId { get; private set; }

    public string TestCode { get; private set; } = null!;

    public string? ResultCode { get; private set; }

    public string RawScoresJson { get; private set; } = "{}";

    public string NormalizedScoresJson { get; private set; } = "{}";

    public string LevelsJson { get; private set; } = "{}";

    public double? CompositeIndex { get; private set; }

    public string FlagsJson { get; private set; } = "[]";

    public int ScoringVersion { get; private set; }

    /// <summary>Natija hisoblangan paytdagi `TestDefinition.Version` (BR-9).</summary>
    public int TestVersion { get; private set; }

    public DateTimeOffset ComputedAt { get; private set; }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private TestResult()
    {
    }

    private TestResult(
        Guid id,
        Guid assessmentTestId,
        Guid assessmentId,
        string testCode,
        string? resultCode,
        string rawScoresJson,
        string normalizedScoresJson,
        string levelsJson,
        double? compositeIndex,
        string flagsJson,
        int scoringVersion,
        int testVersion,
        DateTimeOffset computedAt)
        : base(id)
    {
        AssessmentTestId = assessmentTestId;
        AssessmentId = assessmentId;
        TestCode = testCode;
        ResultCode = resultCode;
        RawScoresJson = rawScoresJson;
        NormalizedScoresJson = normalizedScoresJson;
        LevelsJson = levelsJson;
        CompositeIndex = compositeIndex;
        FlagsJson = flagsJson;
        ScoringVersion = scoringVersion;
        TestVersion = testVersion;
        ComputedAt = computedAt;
    }

    public static TestResult Create(
        Guid id,
        Guid assessmentTestId,
        Guid assessmentId,
        string testCode,
        string rawScoresJson,
        string normalizedScoresJson,
        int scoringVersion,
        int testVersion,
        DateTimeOffset computedAt,
        string? resultCode = null,
        string levelsJson = "{}",
        double? compositeIndex = null,
        string flagsJson = "[]")
    {
        if (string.IsNullOrWhiteSpace(testCode))
        {
            throw new ArgumentException("Test kodi bo'sh bo'lishi mumkin emas.", nameof(testCode));
        }

        return new TestResult(
            id,
            assessmentTestId,
            assessmentId,
            testCode,
            resultCode,
            rawScoresJson,
            normalizedScoresJson,
            levelsJson,
            compositeIndex,
            flagsJson,
            scoringVersion,
            testVersion,
            computedAt);
    }
}
