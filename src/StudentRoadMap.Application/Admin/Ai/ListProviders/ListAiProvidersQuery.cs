using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Ai.ListProviders;

/// <summary>`GET /api/admin/ai/providers` — `docs/07-api-shartnoma.md` §3.5: "Kalitlar maskalangan".</summary>
public sealed record ListAiProvidersQuery : IRequest<Result<IReadOnlyList<AdminAiProviderDto>>>;
