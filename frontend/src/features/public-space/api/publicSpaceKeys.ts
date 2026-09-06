/**
 * `features/public-space` uchun TanStack Query kalitlari — `docs/10` 5.2-bo'lim
 * konvensiyasi, `features/schools/api/schoolsKeys.ts` bilan bir xil naqsh.
 */
export const PUBLIC_SPACE_QUERY_KEYS = {
  /** `GET /api/admin/public-space` — bitta yozuv, parametrsiz. */
  detail: () => ['public-space', 'detail'] as const,
  /** Biriktirish uchun dastur tanlash ro'yxati. */
  programOptions: () => ['public-space', 'program-options'] as const,
};
