namespace StudentRoadMap.Application.Public.CompleteSession;

/// <summary>`docs/07-api-shartnoma.md` 1.8-bo'lim `200` javob shakli.</summary>
public sealed record CompleteSessionResult(string Status, string Message, bool ShowResultToStudent, DateTimeOffset? ResultAvailableAt);
