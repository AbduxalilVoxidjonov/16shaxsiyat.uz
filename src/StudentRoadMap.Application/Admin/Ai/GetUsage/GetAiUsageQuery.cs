using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Ai.GetUsage;

/// <summary>`GET /api/admin/ai/usage?from=&amp;to=` — `docs/07-api-shartnoma.md` §3.5: "Token va taxminiy xarajat statistikasi".</summary>
public sealed record GetAiUsageQuery(DateTimeOffset? From, DateTimeOffset? To) : IRequest<Result<AdminAiUsageDto>>;
