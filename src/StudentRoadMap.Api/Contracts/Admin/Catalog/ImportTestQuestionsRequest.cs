using StudentRoadMap.Application.Admin.Catalog.Questions.Import;

namespace StudentRoadMap.Api.Contracts.Admin.Catalog;

/// <summary>`POST /api/admin/catalog/tests/{id}/questions/import` so'rov tanasi — `prompts/04` seed JSON sxemasidagi `questions[]` bilan bir xil shakl.</summary>
public sealed record ImportTestQuestionsRequest(IReadOnlyList<ImportQuestionItemDto> Questions)
{
    public ImportTestQuestionsCommand ToCommand(Guid testDefinitionId, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(testDefinitionId, Questions, adminUserId, ipAddress, userAgent);
}
