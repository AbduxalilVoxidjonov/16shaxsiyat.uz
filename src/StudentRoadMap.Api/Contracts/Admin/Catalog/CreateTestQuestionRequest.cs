using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Application.Admin.Catalog.Questions.Create;
using StudentRoadMap.Domain.Catalog.Branching;

namespace StudentRoadMap.Api.Contracts.Admin.Catalog;

/// <summary>
/// `POST /api/admin/catalog/tests/{id}/questions` so'rov tanasi — `docs/07` §3.4 (tizim testida
/// `409 SYSTEM_TEST_LOCKED`). `docs/18` §5 kengaytmasi: <see cref="SectionCode"/>,
/// <see cref="Placeholder"/>/<see cref="InputPattern"/>/<see cref="MaxLength"/>,
/// <see cref="MinSelections"/>/<see cref="MaxSelections"/>, <see cref="Visibility"/>,
/// <see cref="Options"/>.
/// </summary>
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
    bool? IsRequired,
    string? SectionCode = null,
    string? Placeholder = null,
    string? InputPattern = null,
    int? MaxLength = null,
    int? MinSelections = null,
    int? MaxSelections = null,
    VisibilityRule? Visibility = null,
    IReadOnlyList<QuestionOptionInputDto>? Options = null)
{
    public CreateTestQuestionCommand ToCommand(Guid testDefinitionId, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(
            testDefinitionId, Code, Order, TextUz, TextRu, TextEn, Type, Scale, Direction, Weight, IsRequired, adminUserId, ipAddress, userAgent,
            SectionCode, Placeholder, InputPattern, MaxLength, MinSelections, MaxSelections, Visibility, Options);
}
