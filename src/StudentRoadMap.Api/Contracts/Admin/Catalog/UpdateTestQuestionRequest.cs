using StudentRoadMap.Application.Admin.Catalog.Questions.Update;

namespace StudentRoadMap.Api.Contracts.Admin.Catalog;

/// <summary>`PUT /api/admin/catalog/questions/{id}` so'rov tanasi — `docs/07` §3.4: tizim savolida faqat `textUz`/`textRu`/`isActive` amalda ta'sir qiladi (`Scale`/`Direction`/`Weight` berilsa domendan `409 SYSTEM_TEST_LOCKED`).</summary>
public sealed record UpdateTestQuestionRequest(
    string TextUz,
    string? TextRu,
    string? TextEn,
    bool IsActive,
    string? Scale,
    int? Direction,
    decimal? Weight,
    int? Order,
    bool? IsRequired)
{
    public UpdateTestQuestionCommand ToCommand(Guid questionId, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(questionId, TextUz, TextRu, TextEn, IsActive, Scale, Direction, Weight, Order, IsRequired, adminUserId, ipAddress, userAgent);
}
