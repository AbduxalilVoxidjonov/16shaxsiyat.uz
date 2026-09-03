import type { AssessmentsListQuery } from '../model/types';

/** `features/assessments` uchun TanStack Query kalitlari — `docs/10`, 5.2-bo'lim konvensiyasi. */
export const ASSESSMENTS_QUERY_KEYS = {
  list: (query: AssessmentsListQuery) => ['assessments', 'list', query] as const,
  detail: (assessmentId: string) => ['assessments', 'detail', assessmentId] as const,
  schoolOptions: () => ['assessments', 'schoolOptions'] as const,
};
