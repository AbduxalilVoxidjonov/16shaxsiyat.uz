using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Dashboard.GetStats;

/// <summary>
/// `GET /api/admin/dashboard/stats?from=&to=` — `docs/07-api-shartnoma.md` 3.6-bo'lim.
/// `From`/`To` — `last30Days` bo'limining oynasi (berilmasa: `now-30kun..now`); `totals` va
/// taqsimotlar (`personalityDistribution`/`activityDistribution`/`hollandTop`) HAR DOIM
/// joriy holatning umumiy (hamma vaqt) ko'rinishi — bular vaqt oynasiga bog'liq emas
/// (o'quvchining "joriy" tipi/faollik darajasi, tarixiy emas).
/// </summary>
public sealed record GetDashboardStatsQuery(DateTimeOffset? From, DateTimeOffset? To) : IRequest<Result<AdminDashboardStatsDto>>;
