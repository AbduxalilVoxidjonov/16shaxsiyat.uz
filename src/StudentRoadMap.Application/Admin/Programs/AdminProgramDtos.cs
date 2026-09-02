namespace StudentRoadMap.Application.Admin.Programs;

/// <summary>
/// `GET /api/admin/programs` ro'yxat elementi — `prompts/34` E15-band (minimal admin API).
/// `scale`/`scaleDirection` bu yerda umuman yo'q — dastur darajasida bunday maydon mavjud emas.
/// </summary>
public sealed record AdminProgramListItemDto(
    Guid Id,
    string Code,
    string NameUz,
    string Kind,
    string Visibility,
    string Status,
    bool IsActive,
    bool IsSystem,
    int DisplayOrder,
    int TestCount);

/// <summary>`GET /api/admin/programs/{id}` — batafsil: tarkib (testlar) va biriktirilgan maktablar.</summary>
public sealed record AdminProgramDetailDto(
    Guid Id,
    string Code,
    string NameUz,
    string? DescriptionUz,
    string Kind,
    string Visibility,
    string Status,
    bool IsActive,
    bool IsSystem,
    int DisplayOrder,
    IReadOnlyList<AdminProgramTestItemDto> Tests,
    IReadOnlyList<Guid> AssignedSchoolIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Dastur tarkibidagi bitta anketa — `docs/07` uslubida `scale`/`scaleDirection`siz.</summary>
public sealed record AdminProgramTestItemDto(Guid TestDefinitionId, string Code, string NameUz, int DisplayOrder);
