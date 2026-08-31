using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// 0..100 oralig'i kafolatlangan foiz qiymati (`docs/04-domain-model.md` §4).
/// Yaratishda oraliqdan tashqari qiymat kelsa `[0,100]` ga qisqartiriladi (clamp) —
/// suzuvchi nuqta arifmetikasidan kelib chiqadigan ±ε chetlanishlarni yutish uchun.
/// </summary>
public sealed class ScorePercent : ValueObject
{
    public const double MinValue = 0.0;

    public const double MaxValue = 100.0;

    public double Value { get; }

    private ScorePercent(double value)
    {
        Value = value;
    }

    /// <summary>Qiymatni `[0,100]` ga qisqartirib (clamp), `docs/03` yaxlitlash qoidasi bilan yaratadi.</summary>
    public static ScorePercent FromClamped(double rawValue, int decimals = ScoringConstants.RoundingDecimals)
    {
        var clamped = Math.Clamp(rawValue, MinValue, MaxValue);
        var rounded = Math.Round(clamped, decimals, MidpointRounding.AwayFromZero);
        return new ScorePercent(rounded);
    }

    public static implicit operator double(ScorePercent scorePercent) => scorePercent.Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString("0.##");
}
