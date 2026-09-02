using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Assessments.Delete;

/// <summary>
/// `DELETE /api/admin/assessments/{id}` — `docs/07-api-shartnoma.md` 3.3-bo'lim: soft delete
/// (`Assessment.MarkDeleted`). `Student`dagi kabi `hard=true` varianti YO'Q — 3.3-bo'lim jadvali
/// faqat "Soft delete" deydi (`Student` uchun bo'lgani kabi maxsus "o'quvchi so'rovi" holati
/// sessiya darajasida yo'q).
/// </summary>
public sealed record DeleteAssessmentCommand(
    Guid Id,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result>;
