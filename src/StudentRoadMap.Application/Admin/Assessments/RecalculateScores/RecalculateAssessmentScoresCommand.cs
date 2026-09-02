using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Assessments.RecalculateScores;

/// <summary>
/// `POST /api/admin/assessments/{id}/recalculate-scores` — `docs/07-api-shartnoma.md` 3.3-bo'lim:
/// "Scoring versiyasi o'zgargan bo'lsa". Saqlangan `Answer`larni O'ZGARTIRMAYDI — faqat
/// `TestResult`/`Assessment.ReliabilityScore` formulalar bilan QAYTA hisoblanadi
/// (`prompts/15` "⚠️ ENG MUHIM": `ReliabilityInputBuilder` orqali xronologik tartib).
/// </summary>
public sealed record RecalculateAssessmentScoresCommand(
    Guid Id,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminRecalculateScoresResultDto>>;
