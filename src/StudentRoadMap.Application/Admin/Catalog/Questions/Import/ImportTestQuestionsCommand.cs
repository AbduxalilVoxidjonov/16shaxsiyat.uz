using MediatR;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Questions.Import;

/// <summary>`prompts/04` seed JSON sxemasidagi savol shakli bilan BIR XIL — `code, order, textUz, type, scale, direction, weight, isRequired?`.</summary>
public sealed record ImportQuestionItemDto(
    string Code,
    int Order,
    string TextUz,
    string Type,
    string Scale,
    int Direction,
    decimal Weight,
    bool? IsRequired);

/// <summary>
/// `POST /api/admin/catalog/tests/{id}/questions/import` — `docs/07` §3.4: "`Custom` — to'liq;
/// tizim — faqat matn yangilash". `Custom` (odatda yangi `Draft`) uchun BARCHA savollar
/// qo'shiladi (`prompts/37` vazifa 7: "natija — yaratilgan test `Draft`" — bu import o'zi
/// testni `Draft`ga o'zgartirmaydi, chaqiruvchi avval `POST /tests` bilan `Draft` yaratadi).
/// Tizim testida FAQAT `Code` bo'yicha moslashgan savollarning matni yangilanadi — yangi savol
/// QO'SHILMAYDI, mos kelmagan qatorlar e'tiborsiz qoldiriladi (`Scale`/`Direction`/`Weight`
/// HAR DOIM e'tiborsiz — BR-8).
/// </summary>
public sealed record ImportTestQuestionsCommand(
    Guid TestDefinitionId,
    IReadOnlyList<ImportQuestionItemDto> Questions,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<CatalogTestDetailDto>>;
