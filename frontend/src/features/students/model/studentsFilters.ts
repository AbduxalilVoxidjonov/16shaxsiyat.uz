import {
  ACTIVITY_LEVEL_VALUES,
  ASSESSMENT_STATUS_VALUES,
  type ActivityLevel,
  type AssessmentStatus,
} from './enums';

/** URL'dagi bo'sh bo'lmagan qiymatlar ro'yxati — chip va "faol filtr" hisoblash uchun. */
export const STUDENTS_FILTER_KEYS = [
  'search',
  'schoolId',
  'grade',
  'status',
  'personalityType',
  'activityLevel',
  'needsAttention',
  'from',
  'to',
] as const;
export type StudentsFilterKey = (typeof STUDENTS_FILTER_KEYS)[number];

export interface StudentsFilterValues {
  search: string;
  schoolId: string;
  /** `''` — hammasi, aks holda `'1'`..`'11'`. */
  grade: string;
  status: AssessmentStatus | '';
  personalityType: string;
  activityLevel: ActivityLevel | '';
  needsAttention: boolean;
  /** `YYYY-MM-DD` yoki `''`. */
  from: string;
  to: string;
}

function readEnum<T extends string>(
  searchParams: URLSearchParams,
  key: string,
  allowed: readonly T[],
): T | '' {
  const value = searchParams.get(key);
  return value && (allowed as readonly string[]).includes(value) ? (value as T) : '';
}

/**
 * URL query'dan joriy filtr qiymatlarini o'qiydi — `SchoolFiltersBar`dagi
 * `readSchoolsFilters` bilan bir xil naqsh (P23). `StudentsPage.tsx` va
 * `StudentFiltersBar.tsx` ikkalasida ham ishlatiladi.
 */
export function readStudentsFilters(searchParams: URLSearchParams): StudentsFilterValues {
  const grade = searchParams.get('grade');
  const gradeNumber = grade ? Number(grade) : NaN;
  return {
    search: searchParams.get('search') ?? '',
    schoolId: searchParams.get('schoolId') ?? '',
    grade: Number.isInteger(gradeNumber) && gradeNumber >= 1 && gradeNumber <= 11 ? grade! : '',
    status: readEnum(searchParams, 'status', ASSESSMENT_STATUS_VALUES),
    personalityType: (searchParams.get('personalityType') ?? '').toUpperCase(),
    activityLevel: readEnum(searchParams, 'activityLevel', ACTIVITY_LEVEL_VALUES),
    needsAttention: searchParams.get('needsAttention') === 'true',
    from: searchParams.get('from') ?? '',
    to: searchParams.get('to') ?? '',
  };
}

/** Joriy filtrlardan kamida bittasi tanlanganmi — "Hammasini tozalash" tugmasini ko'rsatish uchun. */
export function hasActiveStudentsFilters(filters: StudentsFilterValues): boolean {
  return (
    filters.search !== '' ||
    filters.schoolId !== '' ||
    filters.grade !== '' ||
    filters.status !== '' ||
    filters.personalityType !== '' ||
    filters.activityLevel !== '' ||
    filters.needsAttention ||
    filters.from !== '' ||
    filters.to !== ''
  );
}
