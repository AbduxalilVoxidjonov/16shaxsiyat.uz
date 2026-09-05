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
  /**
   * `docs/07` 1.10-bo'lim — `features/marketing/api/useTypeCatalog.ts`. Parametrsiz: kontent
   * hamma uchun bir xil (ochiq marketing kontenti, sessiyaga bog'liq emas).
   */
  publicTypeCatalog: () => ['public', 'type-catalog'] as const,
  /**
   * `docs/07` §2a.2 + §5.1 — `features/public-account/api/usePublicSession.ts`. Sahifa
   * yangilanganda sessiyani tiklash (refresh → `GET /api/me`) natijasi; parametrsiz, chunki
   * bir vaqtda bitta ommaviy foydalanuvchi sessiyasi bo'ladi.
   */
  publicUserSession: () => ['public-user', 'session'] as const,
  /** `docs/07` §5.2 — `features/public-account/api/useMyAssessments.ts`. */
  publicUserAssessments: () => ['public-user', 'assessments'] as const,
  /** `docs/07` §5.3 — `features/public-account/api/useMyAssessmentResult.ts`. */
  publicUserAssessmentResult: (assessmentId: string) =>
    ['public-user', 'assessment-result', assessmentId] as const,
} as const;
