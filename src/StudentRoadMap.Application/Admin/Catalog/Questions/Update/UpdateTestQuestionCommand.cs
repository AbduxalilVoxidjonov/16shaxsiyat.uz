using MediatR;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Questions.Update;

/// <summary>
/// `PUT /api/admin/catalog/questions/{id}` — `docs/07` §3.4: tizim savolida "⚠️ faqat `textUz`,
/// `textRu`, `isActive`". Bitta buyruq ikkalasini ham bajaradi: `Scale`/`Direction`/`Weight`
/// FAQAT berilgan bo'lsagina (`null` emas) domenga uzatiladi — `Question.UpdateScale` `IsSystem`
/// bo'lsa o'zi `SYSTEM_TEST_LOCKED` (BR-8) otadi, shu sabab bu yerda alohida tekshiruv shart
/// emas. `Order` esa BR-8 doirasiga kirmaydi (`Question.UpdateOrder`) — tizim savolida ham ochiq.
/// </summary>
public sealed record UpdateTestQuestionCommand(
    Guid QuestionId,
    string TextUz,
    string? TextRu,
    string? TextEn,
    bool IsActive,
    string? Scale,
    int? Direction,
    decimal? Weight,
    int? Order,
    bool? IsRequired,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<CatalogQuestionItemDto>>;
