import type { AuditListQuery } from '../model/types';

/** `features/audit` uchun TanStack Query kalitlari — docs/10, 5.2-bo'lim konvensiyasi. */
export const AUDIT_QUERY_KEYS = {
  list: (query: AuditListQuery) => ['audit', 'list', query] as const,
};
