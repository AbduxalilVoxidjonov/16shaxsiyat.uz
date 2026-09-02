using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Assessments.GetAnswers;

/// <summary>
/// `GET /api/admin/assessments/{id}/answers?testCode=` — `docs/07-api-shartnoma.md` 3.3-bo'lim:
/// "Xom javoblar (audit uchun)". `TestCode` — ixtiyoriy, berilsa faqat shu test blokining
/// javoblari qaytadi (masalan `"MBTI16"`); mos test sessiyaga biriktirilmagan bo'lsa — bo'sh
/// ro'yxat (xato emas, `prompts/15`da bu holat aniqlanmagan — yumshoq yechim tanlandi).
/// </summary>
public sealed record GetAssessmentAnswersQuery(Guid Id, string? TestCode) : IRequest<Result<IReadOnlyList<AdminAssessmentAnswerDto>>>;
