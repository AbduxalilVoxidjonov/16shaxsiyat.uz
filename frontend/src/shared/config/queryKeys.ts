/**
 * TanStack Query kalitlari — docs/10-frontend-arxitektura.md, 5.2-bo'lim.
 * Har bir feature o'z kalitlarini shu obyektga qo'shadi (skelet bosqichida bo'sh).
 */
export const QUERY_KEYS = {
  // Misol: schools: (f: SchoolFilters) => ['schools', f] as const,
  /** `docs/07` 1.1-bo'lim — `features/public-assessment/api/useSchoolInfo.ts`. */
  publicSchoolInfo: (slug: string, accessToken: string) =>
    ['public', 'school-info', slug, accessToken] as const,
  /** `docs/07` 1.3-bo'lim — `features/public-assessment/api/useSessionState.ts`. */
  publicSessionMe: () => ['public', 'session-me'] as const,
  /** `docs/07` 1.5-bo'lim — `features/public-assessment/api/useTestQuestions.ts`. */
  publicTestQuestions: (testCode: string, page: number) =>
    ['public', 'test-questions', testCode, page] as const,
  /** `docs/07` 1.9-bo'lim — `features/public-assessment/api/useStudentResult.ts`. */
  publicStudentResult: () => ['public', 'student-result'] as const,
} as const;
