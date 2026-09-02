using MediatR;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Questions.Create;

/// <summary>`POST /api/admin/catalog/tests/{id}/questions` — `docs/07` §3.4: tizim testida `409 SYSTEM_TEST_LOCKED` (domendan, BR-8).</summary>
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
    string? UserAgent = null) : IRequest<Result<CatalogQuestionItemDto>>;
