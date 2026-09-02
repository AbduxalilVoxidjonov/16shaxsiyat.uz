using MediatR;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Ai.RerunAnalysis;

/// <summary>
/// `POST /api/admin/assessments/{id}/rerun-analysis` — `docs/07-api-shartnoma.md` §3.3:
/// `{ "provider": "Anthropic", "promptVersion": "v1.1" }` → `202`. `prompts/18` vazifa #4:
/// "eski `IsCurrent` faqat yangisi muvaffaqiyatli bo'lgach o'zgaradi" — bu `AnalysisOrchestrator`
/// zimmasida (`AnalysisOrchestrator.RunAsync`), bu yerda faqat navbatga qo'yiladi.
/// </summary>
public sealed record RerunAnalysisCommand(
    Guid AssessmentId,
    AiProvider? Provider,
    string? PromptVersion,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<RerunAnalysisResultDto>>;
