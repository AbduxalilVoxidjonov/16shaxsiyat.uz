namespace StudentRoadMap.Application.Public.CompleteTest;

/// <summary>`docs/07-api-shartnoma.md` 1.7-bo'lim `200` javob shakli.</summary>
public sealed record CompleteTestResult(string TestCode, string Status, string? NextTestCode, bool AllTestsCompleted);
