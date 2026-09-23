import type { PublicSpaceUsersQuery } from '../model/types';

/**
 * `features/public-space` uchun TanStack Query kalitlari — `docs/10` 5.2-bo'lim
 * konvensiyasi, `features/schools/api/schoolsKeys.ts` bilan bir xil naqsh.
 */
export const PUBLIC_SPACE_QUERY_KEYS = {
  /** `GET /api/admin/public-space` — bitta yozuv, parametrsiz. */
  detail: () => ['public-space', 'detail'] as const,
  /** Biriktirish uchun test tanlash ro'yxati (`GET /api/admin/catalog/tests`, 2026-09-23). */
  testOptions: () => ['public-space', 'test-options'] as const,
  /** `GET /api/admin/public-space/users` — sahifalangan foydalanuvchilar ro'yxati. */
  users: (query: PublicSpaceUsersQuery) => ['public-space', 'users', query] as const,
};
