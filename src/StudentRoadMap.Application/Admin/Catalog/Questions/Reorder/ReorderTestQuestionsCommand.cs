using MediatR;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Questions.Reorder;

/// <summary>`POST /api/admin/catalog/tests/{id}/questions/reorder` — `docs/07` §3.4 (`[{id, displayOrder}]`), tizim testida ham ✅ (BR-8 tartibni qulflamaydi).</summary>
public sealed record ReorderQuestionItem(Guid Id, int DisplayOrder);

public sealed record ReorderTestQuestionsCommand(
    Guid TestDefinitionId,
    IReadOnlyList<ReorderQuestionItem> Items,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<IReadOnlyList<CatalogQuestionItemDto>>>;
