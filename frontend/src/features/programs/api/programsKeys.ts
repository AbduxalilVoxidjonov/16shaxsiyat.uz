import type { ProgramsListQuery } from '../model/types';

/** `features/programs` uchun TanStack Query kalitlari — `docs/10`, 5.2-bo'lim konvensiyasi. */
export const PROGRAMS_QUERY_KEYS = {
  list: (query: ProgramsListQuery) => ['programs', 'list', query] as const,
  detail: (id: string) => ['programs', 'detail', id] as const,
};
