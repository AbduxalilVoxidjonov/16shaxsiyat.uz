namespace StudentRoadMap.Application.Seeding;

/// <summary>
/// `Infrastructure/Persistence/SeedData/test-definitions/*.json` dagi bitta savol yozuvi —
/// maydonlar `docs/03-psixologik-metodikalar.md` 1-bo'limi va `prompts/04` sxemasiga mos.
/// System.Text.Json orqali (case-insensitive) to'g'ridan-to'g'ri deserializatsiya qilinadi.
/// </summary>
public sealed record QuestionSeedDto
{
    public string Code { get; init; } = string.Empty;

    public int Order { get; init; }

    public string TextUz { get; init; } = string.Empty;

    public string? TextRu { get; init; }

    public string? TextEn { get; init; }

    /// <summary>`QuestionType` enum nomi — masalan "Likert5" (`docs/05` 3-bo'lim).</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Shkala kodi — masalan `EI`, `O`, `R`, `MOT` (`docs/03` 1-bo'lim).</summary>
    public string Scale { get; init; } = string.Empty;

    /// <summary>+1 yoki -1.</summary>
    public int Direction { get; init; } = 1;

    public decimal Weight { get; init; } = 1.0m;

    public bool IsRequired { get; init; } = true;
}
