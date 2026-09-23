import type { SchoolsListQuery } from '../model/types';

/**
 * `features/schools` uchun TanStack Query kalitlari — docs/10-frontend-arxitektura.md,
 * 5.2-bo'lim konvensiyasi (`QUERY_KEYS.schools(filters)` misoli).
 */
export const SCHOOLS_QUERY_KEYS = {
  list: (query: SchoolsListQuery) => ['schools', 'list', query] as const,
  detail: (id: string) => ['schools', 'detail', id] as const,
  /** `GET /api/admin/schools/link-health` — dashboard banneri (`docs/07` 3.1, 2026-09-03). */
  linkHealth: () => ['schools', 'link-health'] as const,
  /** Maktab formasidagi "Testlar" tanlovi — `GET /api/admin/catalog/tests` (2026-09-23). */
  testOptions: () => ['schools', 'test-options'] as const,
};
