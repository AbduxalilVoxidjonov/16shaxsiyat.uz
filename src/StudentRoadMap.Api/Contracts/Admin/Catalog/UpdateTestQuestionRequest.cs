using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Application.Admin.Catalog.Questions.Update;
using StudentRoadMap.Domain.Catalog.Branching;

namespace StudentRoadMap.Api.Contracts.Admin.Catalog;

/// <summary>
/// `PUT /api/admin/catalog/questions/{id}` so'rov tanasi — `docs/07` §3.4: tizim savolida faqat
/// `textUz`/`textRu`/`isActive` amalda ta'sir qiladi (`Scale`/`Direction`/`Weight` va `docs/18`
/// §5 yangi maydonlari berilsa ham e'tiborsiz — `UpdateTestQuestionCommand` izohiga qarang).
/// </summary>
public sealed record UpdateTestQuestionRequest(
    string TextUz,
    string? TextRu,
    string? TextEn,
    bool IsActive,
    string? Scale,
    int? Direction,
    decimal? Weight,
    int? Order,
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
    public UpdateTestQuestionCommand ToCommand(Guid questionId, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(
            questionId, TextUz, TextRu, TextEn, IsActive, Scale, Direction, Weight, Order, IsRequired, adminUserId, ipAddress, userAgent,
            SectionCode, Placeholder, InputPattern, MaxLength, MinSelections, MaxSelections, Visibility, Options);
}
