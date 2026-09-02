/**
 * Audit filtrlari (`action`, `entityType`, `from`, `to`) — URL query'da (`docs/10`, 5.3-bo'lim;
 * `prompts/28` MAXSUS DIQQAT #6: "filtr ... URL'da holat"). `dashboard/model/dateRangeFilters.ts`
 * bilan bir xil naqsh: format `YYYY-MM-DD` yoki `''` (chegara yo'q).
 */
export interface AuditFilterValues {
  action: string;
  entityType: string;
  from: string;
  to: string;
}

export function readAuditFilters(searchParams: URLSearchParams): AuditFilterValues {
  return {
    action: searchParams.get('action') ?? '',
    entityType: searchParams.get('entityType') ?? '',
    from: searchParams.get('from') ?? '',
    to: searchParams.get('to') ?? '',
  };
}

export function hasActiveAuditFilters(filters: AuditFilterValues): boolean {
  return filters.action !== '' || filters.entityType !== '' || filters.from !== '' || filters.to !== '';
}
