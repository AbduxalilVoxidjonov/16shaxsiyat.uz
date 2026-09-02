using MediatR;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Ai.SetDefaultProvider;

/// <summary>`POST /api/admin/ai/providers/{provider}/set-default` — `docs/07-api-shartnoma.md` §3.5.</summary>
public sealed record SetDefaultAiProviderCommand(
    AiProvider Provider,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminAiProviderDto>>;
