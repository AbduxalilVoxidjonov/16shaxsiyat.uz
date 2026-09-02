import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { AdminAuditLogItemDtoPagedResult, AuditListQuery } from '../model/types';
import { AUDIT_QUERY_KEYS } from './auditKeys';

const LIST_STALE_TIME_MS = 30_000;

function buildQueryString(query: AuditListQuery): string {
  const params = new URLSearchParams();
  if (query.action) params.set('action', query.action);
  if (query.entityType) params.set('entityType', query.entityType);
  if (query.from) params.set('from', query.from);
  if (query.to) params.set('to', query.to);
  params.set('page', String(query.page));
  params.set('pageSize', String(query.pageSize));
  return params.toString();
}

/** `GET /api/admin/audit-logs` — docs/07, 3.6-bo'lim. */
export function useAuditLogsQuery(query: AuditListQuery) {
  return useQuery({
    queryKey: AUDIT_QUERY_KEYS.list(query),
    queryFn: ({ signal }) =>
      adminRequest<AdminAuditLogItemDtoPagedResult>(`/api/admin/audit-logs?${buildQueryString(query)}`, {
        signal,
      }),
    staleTime: LIST_STALE_TIME_MS,
    placeholderData: (previous) => previous,
  });
}
