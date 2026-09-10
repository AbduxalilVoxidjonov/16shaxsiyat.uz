using MediatR;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Domain.Catalog.Branching;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Questions.Update;

/// <summary>
/// `PUT /api/admin/catalog/questions/{id}` — `docs/07` §3.4: tizim savolida "⚠️ faqat `textUz`,
/// `textRu`, `isActive`". Bitta buyruq ikkalasini ham bajaradi: `Scale`/`Direction`/`Weight`
/// FAQAT berilgan bo'lsagina (`null` emas) domenga uzatiladi — `Question.UpdateScale` `IsSystem`
/// bo'lsa o'zi `SYSTEM_TEST_LOCKED` (BR-8) otadi, shu sabab bu yerda alohida tekshiruv shart
/// emas. `Order` esa BR-8 doirasiga kirmaydi (`Question.UpdateOrder`) — tizim savolida ham ochiq.
///
/// <para>
/// `docs/18` §5 kengaytmasi — `Scale`/`Direction`/`Weight`dan FARQLI: <see cref="SectionCode"/>
/// (`null` — bo'limsiz), <see cref="Placeholder"/>, <see cref="InputPattern"/>,
/// <see cref="MaxLength"/>, <see cref="MinSelections"/>/<see cref="MaxSelections"/>,
/// <see cref="Visibility"/> — bular `IsSystem = false` savolda HAR DOIM TO'LIQ qo'llaniladi
/// (forma butun holatni qayta yuboradi, `null` — "tozalash"/"bo'limsiz" degani). Tizim savolida
/// (`IsSystem = true`) bu maydonlarga umuman TEGILMAYDI — so'rovda ular BERILGAN bo'lsa ham
/// (BR-8, "avvalgidek faqat `textUz`/`textRu`/`isActive`"). <see cref="Options"/> — `null` bo'lsa
/// TEGILMAYDI, ro'yxat (bo'sh bo'lsa ham) berilsa TO'LIQ almashtiradi (`docs/18` §5: "tahrirlashda
/// to'liq almashtirish").
/// </para>
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
    string? UserAgent = null,
    string? SectionCode = null,
    string? Placeholder = null,
    string? InputPattern = null,
    int? MaxLength = null,
    int? MinSelections = null,
    int? MaxSelections = null,
    VisibilityRule? Visibility = null,
    IReadOnlyList<QuestionOptionInputDto>? Options = null) : IRequest<Result<CatalogQuestionItemDto>>;
