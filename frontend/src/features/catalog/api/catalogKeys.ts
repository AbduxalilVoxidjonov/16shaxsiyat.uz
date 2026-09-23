/** `features/catalog` uchun TanStack Query kalitlari — `docs/10`, 5.2-bo'lim konvensiyasi. */
export const CATALOG_QUERY_KEYS = {
  list: () => ['catalog', 'tests', 'list'] as const,
  detail: (id: string) => ['catalog', 'tests', 'detail', id] as const,
  questions: (id: string) => ['catalog', 'tests', 'questions', id] as const,
  scales: (id: string) => ['catalog', 'tests', 'scales', id] as const,
  /** `docs/18` §5 — bo'limlar ro'yxati (`GET /api/admin/catalog/tests/{id}/sections`). */
  sections: (id: string) => ['catalog', 'tests', 'sections', id] as const,
  /**
   * `docs/07` §3.4.1 (2026-09-23) — test biriktirmasi (`GET .../tests/{id}/assignment`).
   * Test holati o'zgarsa (`publish`/`toggle-active`/`archive`) `state` ham o'zgaradi — shu
   * sabab lifecycle mutatsiyalari buni ham yangilaydi.
   */
  assignment: (id: string) => ['catalog', 'tests', 'assignment', id] as const,
  /** Biriktirish kartasidagi maktab qidiruvi (`GET /api/admin/schools?search=`). */
  schoolOptions: (search: string) => ['catalog', 'school-options', search] as const,
  /** Biriktirilgan maktab chiplari uchun nomlar (`GET /api/admin/schools/{id}` × N). */
  schoolsByIds: (ids: readonly string[]) =>
    ['catalog', 'schools-by-ids', [...ids].sort()] as const,
};
