/** `features/catalog` uchun TanStack Query kalitlari — `docs/10`, 5.2-bo'lim konvensiyasi. */
export const CATALOG_QUERY_KEYS = {
  list: () => ['catalog', 'tests', 'list'] as const,
  detail: (id: string) => ['catalog', 'tests', 'detail', id] as const,
  questions: (id: string) => ['catalog', 'tests', 'questions', id] as const,
  scales: (id: string) => ['catalog', 'tests', 'scales', id] as const,
  /** `docs/18` §5 — bo'limlar ro'yxati (`GET /api/admin/catalog/tests/{id}/sections`). */
  sections: (id: string) => ['catalog', 'tests', 'sections', id] as const,
};
