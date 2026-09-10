using MediatR;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Domain.Catalog.Branching;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Questions.Create;

/// <summary>
/// `POST /api/admin/catalog/tests/{id}/questions` — `docs/07` §3.4: tizim testida `409
/// SYSTEM_TEST_LOCKED` (domendan, BR-8). `docs/18` §5 kengaytmasi: <see cref="SectionCode"/>
/// (yoki `null`), <see cref="Placeholder"/>/<see cref="InputPattern"/>/<see cref="MaxLength"/>
/// (matn turlari), <see cref="MinSelections"/>/<see cref="MaxSelections"/> (`MultiChoice`),
/// <see cref="Visibility"/> (B-2 — faqat `Survey`), <see cref="Options"/> (`SingleChoice`/
/// `ForcedChoice`/`MultiChoice`).
/// </summary>
public sealed record CreateTestQuestionCommand(
    Guid TestDefinitionId,
    string Code,
    int Order,
    string TextUz,
    string? TextRu,
    string? TextEn,
    string Type,
    string Scale,
    int Direction,
    decimal Weight,
    bool? IsRequired,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null,
    string? SectionCode = null,
    string? Placeholder = null,
    string? InputPattern = null,
    int? MaxLength = null,
    int? MinSelections = null,
    int? MaxSelections = null,
    VisibilityRule? Visibility = null,
    IReadOnlyList<QuestionOptionInputDto>? Options = null) : IRequest<Result<CatalogQuestionItemDto>>;
