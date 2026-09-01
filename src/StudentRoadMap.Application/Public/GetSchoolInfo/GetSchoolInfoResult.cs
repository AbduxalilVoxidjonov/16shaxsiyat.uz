namespace StudentRoadMap.Application.Public.GetSchoolInfo;

/// <summary>`docs/07-api-shartnoma.md` 1.1-bo'lim javob shakli.</summary>
public sealed record GetSchoolInfoResult(
    Guid SchoolId,
    string Name,
    string Region,
    string District,
    bool RequiresAccessCode,
    IReadOnlyList<PublicTestCatalogItemDto> Tests,
    int TotalEstimatedMinutes,
    string ConsentText);

/// <summary>Boshlanish ekranidagi bitta test bloki haqida ma'lumot (savol soni, taxminiy vaqt).</summary>
public sealed record PublicTestCatalogItemDto(
    string Code,
    string Name,
    int QuestionCount,
    int EstimatedMinutes,
    int Order);
