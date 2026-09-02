using StudentRoadMap.Application.Admin.Catalog.Questions.Create;

namespace StudentRoadMap.Api.Contracts.Admin.Catalog;

/// <summary>`POST /api/admin/catalog/tests/{id}/questions` so'rov tanasi — `docs/07` §3.4 (tizim testida `409 SYSTEM_TEST_LOCKED`).</summary>
public sealed record CreateTestQuestionRequest(
    string Code,
    int Order,
    string TextUz,
    string? TextRu,
    string? TextEn,
    string Type,
    string Scale,
    int Direction,
    decimal Weight,
    bool? IsRequired)
{
    public CreateTestQuestionCommand ToCommand(Guid testDefinitionId, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(testDefinitionId, Code, Order, TextUz, TextRu, TextEn, Type, Scale, Direction, Weight, IsRequired, adminUserId, ipAddress, userAgent);
}
