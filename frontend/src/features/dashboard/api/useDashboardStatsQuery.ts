import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { DashboardStatsQuery, DashboardStatsResponse } from '../model/types';
import { DASHBOARD_QUERY_KEYS } from './dashboardKeys';

/** `docs/10`, 5.2-bo'lim: "staleTime: ... dashboard 60s". */
const DASHBOARD_STALE_TIME_MS = 60_000;

function buildQueryString(query: DashboardStatsQuery): string {
  const params = new URLSearchParams();
  if (query.from) params.set('from', query.from);
  if (query.to) params.set('to', query.to);
  return params.toString();
}

/** `GET /api/admin/dashboard/stats?from=&to=` — docs/07, 3.6-bo'lim. */
export function useDashboardStatsQuery(query: DashboardStatsQuery) {
  const queryString = buildQueryString(query);
  return useQuery({
    queryKey: DASHBOARD_QUERY_KEYS.stats(query),
    queryFn: ({ signal }) =>
      adminRequest<DashboardStatsResponse>(
        `/api/admin/dashboard/stats${queryString ? `?${queryString}` : ''}`,
        { signal },
      ),
    staleTime: DASHBOARD_STALE_TIME_MS,
    placeholderData: (previous) => previous,
  });
}
