using StudentRoadMap.Application.Admin.Catalog.Questions.Reorder;

namespace StudentRoadMap.Api.Contracts.Admin.Catalog;

/// <summary>`POST /api/admin/catalog/tests/{id}/questions/reorder` so'rov tanasi — `docs/07` §3.4 (`[{id, displayOrder}]`).</summary>
public sealed record ReorderTestQuestionsRequest(IReadOnlyList<ReorderQuestionItem> Items)
{
    public ReorderTestQuestionsCommand ToCommand(Guid testDefinitionId, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(testDefinitionId, Items, adminUserId, ipAddress, userAgent);
}
