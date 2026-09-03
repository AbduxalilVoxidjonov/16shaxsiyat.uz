import type { StudentsListQuery } from '../model/types';

/**
 * `features/students` uchun TanStack Query kalitlari — docs/10-frontend-arxitektura.md,
 * 5.2-bo'lim konvensiyasi (`QUERY_KEYS.students(filters)` misoli), xuddi
 * `features/schools/api/schoolsKeys.ts` (P23) bilan bir xil naqsh.
 */
export const STUDENTS_QUERY_KEYS = {
  list: (query: StudentsListQuery) => ['students', 'list', query] as const,
  schoolOptions: (search: string) => ['students', 'schoolOptions', search] as const,
  schoolName: (schoolId: string) => ['students', 'schoolName', schoolId] as const,
  /** Individual profil (P25) — docs/10, 5.2-bo'lim: "profil 0" (`staleTime`, har doim yangi). */
  profile: (studentId: string) => ['students', 'profile', studentId] as const,
  rawAnswers: (assessmentId: string) => ['students', 'rawAnswers', assessmentId] as const,
};
