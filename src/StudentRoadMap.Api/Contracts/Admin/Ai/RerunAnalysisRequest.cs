using StudentRoadMap.Application.Admin.Ai.RerunAnalysis;
using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Api.Contracts.Admin.Ai;

/// <summary>`POST /api/admin/assessments/{id}/rerun-analysis` so'rov tanasi — `docs/07` §3.3: "{ provider?, promptVersion? }".</summary>
public sealed record RerunAnalysisRequest(AiProvider? Provider, string? PromptVersion)
{
    public RerunAnalysisCommand ToCommand(Guid assessmentId, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(assessmentId, Provider, PromptVersion, adminUserId, ipAddress, userAgent);
}
