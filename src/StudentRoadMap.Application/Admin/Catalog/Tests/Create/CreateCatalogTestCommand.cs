using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Create;

/// <summary>
/// `POST /api/admin/catalog/tests` — `docs/07` §3.4: "Yangi `Custom` anketa — `Draft` holatida
/// yaratiladi". Har doim `Kind = Custom`/`IsSystem = false` — tizim metodikasi faqat seed orqali
/// keladi (`prompts/04`). `ScoringMode` ixtiyoriy (standart `Scored`/`SUM`) — `docs/06` §8
/// 2026-09-02 qarori: "Ikkalasi ham kerak" (`Scored` va `Survey`).
/// </summary>
public sealed record CreateCatalogTestCommand(
    string Code,
    string NameUz,
    string? DescriptionUz,
    int EstimatedMinutes,
    int? PageSize,
    bool? ShuffleQuestions,
    int? DisplayOrder,
    string? ScoringMode,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<CatalogTestDetailDto>>;
