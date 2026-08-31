namespace StudentRoadMap.Application.Seeding;

/// <summary>
/// `Infrastructure/Persistence/SeedData/test-definitions/*.json` faylining ildiz sxemasi
/// (`prompts/04-katalog-va-seed-infratuzilma.md`, 1-band). To'rtta tizim metodikasi
/// (`mbti16.json`, `big5.json`, `riasec.json`, `activity.json`) shu shaklga mos.
/// </summary>
public sealed record TestDefinitionSeedDto
{
    /// <summary>Tizim kodi — `MBTI16`/`BIG5`/`RIASEC`/`ACTIVITY`. `ScoringStrategyCode` shundan olinadi.</summary>
    public string Code { get; init; } = string.Empty;

    public string NameUz { get; init; } = string.Empty;

    public string? DescriptionUz { get; init; }

    public int Version { get; init; } = 1;

    public int DisplayOrder { get; init; }

    public int EstimatedMinutes { get; init; }

    public int PageSize { get; init; } = 10;

    public bool ShuffleQuestions { get; init; }

    public IReadOnlyList<QuestionSeedDto> Questions { get; init; } = [];
}
