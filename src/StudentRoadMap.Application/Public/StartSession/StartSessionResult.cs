using StudentRoadMap.Application.Public.Common;

namespace StudentRoadMap.Application.Public.StartSession;

/// <summary>`docs/07-api-shartnoma.md` 1.2-bo'lim `201` javob shakli.</summary>
public sealed record StartSessionResult(
    string SessionToken,
    Guid AssessmentId,
    string Status,
    DateTimeOffset ExpiresAt,
    bool Resumed,
    IReadOnlyList<PublicTestSummaryDto> Tests);
