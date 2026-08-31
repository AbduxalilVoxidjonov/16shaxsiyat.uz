using StudentRoadMap.Domain.Assessments;

namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// `ReliabilityCalculator.Calculate` natijasi — `docs/03-psixologik-metodikalar.md` §7.
/// </summary>
/// <param name="Score">0..100, `clamp(0, 100, 100 − Σ penalties)`.</param>
/// <param name="Flag">`≥70 Reliable · 40–69 Questionable · <40 Unreliable`.</param>
/// <param name="Reasons">Ishga tushgan jarima signallari, sobit tartibda
/// (`AllSameAnswer`, `StraightLining`, `FastAnswers`, `ReverseConflict`, `ShortSession`).</param>
public sealed record ReliabilityResult(double Score, ReliabilityFlag Flag, IReadOnlyList<string> Reasons);
