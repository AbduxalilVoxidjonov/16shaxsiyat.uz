/**
 * Sana oralig'i filtri (`from`/`to`) — URL query'da saqlanadi (vazifa ko'rsatmasi 6-band;
 * `students/model/studentsFilters.ts` bilan bir xil naqsh: `readXFilters(searchParams)`).
 * Format `YYYY-MM-DD` (backend `date` — docs/07, 4-bo'lim) yoki `''` (chegara yo'q).
 */
export interface DashboardDateRangeValues {
  from: string;
  to: string;
}

export function readDashboardDateRange(searchParams: URLSearchParams): DashboardDateRangeValues {
  return {
    from: searchParams.get('from') ?? '',
    to: searchParams.get('to') ?? '',
  };
}

/** Joriy filtrlardan kamida bittasi tanlanganmi. */
export function hasActiveDashboardDateRange(filters: DashboardDateRangeValues): boolean {
  return filters.from !== '' || filters.to !== '';
}
