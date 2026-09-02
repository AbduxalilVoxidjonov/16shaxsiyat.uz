import type { DashboardStatsQuery } from '../model/types';

/**
 * `features/dashboard` uchun TanStack Query kaliti — docs/10-frontend-arxitektura.md,
 * 5.2-bo'lim: `QUERY_KEYS.dashboard(f: DateRange)`. `schoolsKeys.ts`/`studentsKeys.ts`
 * (P23/P24) bilan bir xil naqsh.
 */
export const DASHBOARD_QUERY_KEYS = {
  stats: (query: DashboardStatsQuery) => ['dashboard', 'stats', query] as const,
};
