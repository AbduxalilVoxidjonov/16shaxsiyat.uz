using MediatR;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Domain.Catalog.Branching;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Sections.Update;

/// <summary>
/// `PUT /api/admin/catalog/sections/{sectionId}` — `docs/18` §5. `Code` o'zgarmaydi (yaratishda
/// belgilanadi, `QuestionSection`da `UpdateCode` yo'q). Tizim metodikasida `409
/// SYSTEM_TEST_LOCKED` — amalda bo'lim hech qachon tizim testida mavjud bo'lmaydi (B-3), lekin
/// himoya baribir qo'lda tekshiriladi (`Question.UpdateVisibility` bilan bir xil sabab —
/// `QuestionSection.UpdateMetadata` `IsSystem`ni TEKSHIRMAYDI).
/// </summary>
public sealed record UpdateTestSectionCommand(
    Guid SectionId,
    string TitleUz,
    string? DescriptionUz,
    VisibilityRule? Visibility,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<CatalogSectionItemDto>>;
