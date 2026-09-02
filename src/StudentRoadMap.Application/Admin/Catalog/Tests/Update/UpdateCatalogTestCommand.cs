using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Update;

/// <summary>
/// `PUT /api/admin/catalog/tests/{id}` — `docs/07` §3.4: "Nom, tavsif, tartib, `pageSize`,
/// `shuffleQuestions`". `Code`/`Kind`/`IsSystem`/`Scale`/savollar O'ZGARMAYDI — bu yerda umuman
/// yo'q. Tizim metodikasida ham RUXSAT ETILGAN (BR-8 faqat savol soni/`Scale`/`Direction`ni
/// qulflaydi, `TestDefinition.UpdateMetadata` IsSystem tekshiruvi qilmaydi).
/// </summary>
public sealed record UpdateCatalogTestCommand(
    Guid Id,
    string NameUz,
    string? DescriptionUz,
    int DisplayOrder,
    int EstimatedMinutes,
    bool ShuffleQuestions,
    int PageSize,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<CatalogTestDetailDto>>;
