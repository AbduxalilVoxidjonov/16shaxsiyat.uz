/**
 * `features/ai-settings` uchun TanStack Query kalitlari — docs/10-frontend-arxitektura.md,
 * 5.2-bo'lim konvensiyasi (`QUERY_KEYS.schools(filters)` misoli).
 */
export const AI_SETTINGS_QUERY_KEYS = {
  providers: () => ['ai-settings', 'providers'] as const,
  prompts: () => ['ai-settings', 'prompts'] as const,
  usage: (from: string, to: string) => ['ai-settings', 'usage', from, to] as const,
};
