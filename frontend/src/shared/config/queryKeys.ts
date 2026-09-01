/**
 * TanStack Query kalitlari — docs/10-frontend-arxitektura.md, 5.2-bo'lim.
 * Har bir feature o'z kalitlarini shu obyektga qo'shadi (skelet bosqichida bo'sh).
 */
export const QUERY_KEYS = {
  // Misol: schools: (f: SchoolFilters) => ['schools', f] as const,
} as const;
