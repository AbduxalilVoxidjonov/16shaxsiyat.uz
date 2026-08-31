using System.Text.Json.Serialization;

namespace StudentRoadMap.Domain.Tests.Scoring;

/// <summary>Bitta oltin holat — `tests/.../Scoring/GoldenCases/*.json` faylidagi massiv elementi.</summary>
public sealed class GoldenCaseFile
{
    public string Name { get; set; } = "";

    public string StrategyCode { get; set; } = "";

    public double Tolerance { get; set; } = 0.1;

    public List<GoldenQuestion> Questions { get; set; } = [];

    /// <summary>Faqat `SUM` strategiyasi uchun: shkala kodi → talqin oraliqlari.</summary>
    public Dictionary<string, List<GoldenBand>>? ScaleBands { get; set; }

    public GoldenExpected Expected { get; set; } = new();

    public override string ToString() => Name;
}

public sealed class GoldenQuestion
{
    public string Code { get; set; } = "";

    public string Scale { get; set; } = "";

    public int Direction { get; set; } = 1;

    public double Weight { get; set; } = 1.0;

    public string QuestionType { get; set; } = "Likert5";

    public int DisplayOrder { get; set; }

    public int Answer { get; set; }
}

public sealed class GoldenBand
{
    public double Min { get; set; }

    public double Max { get; set; }

    public string Level { get; set; } = "";
}

/// <summary>
/// Kutilgan natija — faqat JSON'da ko'rsatilgan (`null` bo'lmagan) maydonlar tekshiriladi.
/// </summary>
public sealed class GoldenExpected
{
    public string? ResultCode { get; set; }

    public Dictionary<string, double>? NormalizedScores { get; set; }

    public Dictionary<string, double>? RawScores { get; set; }

    public Dictionary<string, string>? Levels { get; set; }

    public double? CompositeIndex { get; set; }

    public List<string>? Flags { get; set; }

    public string? InterpretationKey { get; set; }
}
