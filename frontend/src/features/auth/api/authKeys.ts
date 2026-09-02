/** `features/auth` uchun TanStack Query kalitlari — docs/10, 5.2-bo'lim konvensiyasi. */
export const AUTH_QUERY_KEYS = {
  me: () => ['auth', 'me'] as const,
};
