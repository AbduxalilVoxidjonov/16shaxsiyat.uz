using StudentRoadMap.Application.Public.Common;

namespace StudentRoadMap.Application.Public.GetSession;

/// <summary>`docs/07-api-shartnoma.md` 1.3-bo'lim javob shakli.</summary>
public sealed record GetSessionStateResult(
    Guid AssessmentId,
    string Status,
    PublicStudentSummaryDto Student,
    DateTimeOffset ExpiresAt,
    string? CurrentTestCode,
    IReadOnlyList<PublicTestSummaryDto> Tests,
    int ProgressPercent);

/// <summary>O'quvchining to'liq ismi hech qachon qaytmaydi (`prompts/10` cheklovi) — faqat qisqartirilgan ism.</summary>
public sealed record PublicStudentSummaryDto(string FirstNameShort, int Grade);
