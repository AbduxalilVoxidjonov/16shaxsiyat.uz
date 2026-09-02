import type { SchoolsListQuery } from '../model/types';

/**
 * `features/schools` uchun TanStack Query kalitlari — docs/10-frontend-arxitektura.md,
 * 5.2-bo'lim konvensiyasi (`QUERY_KEYS.schools(filters)` misoli).
 */
export const SCHOOLS_QUERY_KEYS = {
  list: (query: SchoolsListQuery) => ['schools', 'list', query] as const,
  detail: (id: string) => ['schools', 'detail', id] as const,
};
