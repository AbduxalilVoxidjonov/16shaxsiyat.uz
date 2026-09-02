using MediatR;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Ai.TestProvider;

/// <summary>`POST /api/admin/ai/providers/{provider}/test` — `docs/07-api-shartnoma.md` §3.5: "Aloqa tekshiruvi → { ok, latencyMs, message }".</summary>
public sealed record TestAiProviderCommand(AiProvider Provider, Guid AdminUserId) : IRequest<Result<AdminAiProviderTestResultDto>>;
