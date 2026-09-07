import type { TFunction } from 'i18next';
import {
  ACTIVITY_LEVEL_VALUES,
  ASSESSMENT_STATUS_VALUES,
  GENDER_FILTER_VALUES,
  type ActivityLevel,
  type AssessmentStatus,
  type GenderFilter,
} from './enums';

/** URL'dagi bo'sh bo'lmagan qiymatlar ro'yxati — chip va "faol filtr" hisoblash uchun. */
export const STUDENTS_FILTER_KEYS = [
  'search',
  'schoolId',
  'grade',
  'status',
  'personalityType',
  'activityLevel',
  'gender',
  'ageMin',
  'ageMax',
  'needsAttention',
  'from',
  'to',
] as const;
export type StudentsFilterKey = (typeof STUDENTS_FILTER_KEYS)[number];

/**
 * Chip kaliti — URL kalitidan farqi: yosh oralig'i IKKI URL parametri (`ageMin`/`ageMax`),
 * lekin BITTA chip ("Yosh: 11–14"); olib tashlanganda ikkalasi birga o'chadi.
 */
export type StudentsChipKey = Exclude<StudentsFilterKey, 'ageMin' | 'ageMax'> | 'age';

/** Backend `Student.MinAge..MaxAge` bilan bir xil (`docs/07` 3.2: `ageMin`/`ageMax` 6..99). */
export const STUDENT_AGE_MIN = 6;
export const STUDENT_AGE_MAX = 99;

/** Yosh oralig'i — `ageMin`/`ageMax` ixtiyoriy, kamida bittasi bor. */
export interface AgeRange {
  ageMin?: number;
  ageMax?: number;
}

/**
 * Tayyor yosh oraliqlari (`Select`) — maktab bosqichlariga mos: boshlang'ich (6–10), o'rta
 * (11–14), yuqori (15–17) va kattalar (18+). URL'da har doim `ageMin`/`ageMax` sifatida
 * saqlanadi — chuqur havola (deep-link) ixtiyoriy oraliqni ham qabul qiladi, `Select` shunda
 * o'sha oraliqni qo'shimcha variant sifatida ko'rsatadi (`ageRangeOptionValue`).
 */
export const AGE_RANGE_PRESETS: readonly AgeRange[] = [
  { ageMin: 6, ageMax: 10 },
  { ageMin: 11, ageMax: 14 },
  { ageMin: 15, ageMax: 17 },
  { ageMin: 18 },
];

/** `Select` option qiymati: `"11-14"`, `"18-"` (faqat quyi), `"-14"` (faqat yuqori). */
export function ageRangeOptionValue(range: AgeRange): string {
  return `${range.ageMin === undefined ? '' : String(range.ageMin)}-${range.ageMax === undefined ? '' : String(range.ageMax)}`;
}

/** Yosh oralig'i yorlig'i: "11–14 yosh", "18+ yosh", "14 yoshgacha" — chip va `Select` bir xil matn. */
export function formatAgeRange(range: AgeRange, t: TFunction): string {
  if (range.ageMin !== undefined && range.ageMax !== undefined) {
    return t('students.filters.ageRange', { min: range.ageMin, max: range.ageMax });
  }
  if (range.ageMin !== undefined) {
    return t('students.filters.ageFrom', { min: range.ageMin });
  }
  return t('students.filters.ageTo', { max: range.ageMax });
}

/** `ageRangeOptionValue` ning teskarisi; noto'g'ri satr — `null` (filtr yo'q). */
export function parseAgeRangeOptionValue(value: string): AgeRange | null {
  const [minPart, maxPart] = value.split('-');
  const ageMin = readAge(minPart);
  const ageMax = readAge(maxPart);
  return normalizeAgeRange(ageMin, ageMax);
}

export interface StudentsFilterValues {
  search: string;
  schoolId: string;
  /** `''` — hammasi, aks holda `'1'`..`'11'`. */
  grade: string;
  status: AssessmentStatus | '';
  personalityType: string;
  activityLevel: ActivityLevel | '';
  /** `''` — hammasi. */
  gender: GenderFilter | '';
  /** `null` — yosh filtri yo'q. */
  age: AgeRange | null;
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

/** `6..99` oralig'idagi butun son, aks holda `undefined`. */
function readAge(raw: string | null | undefined): number | undefined {
  if (!raw) return undefined;
  const value = Number(raw);
  return Number.isInteger(value) && value >= STUDENT_AGE_MIN && value <= STUDENT_AGE_MAX
    ? value
    : undefined;
}

/**
 * Ikkala chegara ham bo'sh yoki `ageMin > ageMax` (qo'lda buzilgan URL) — filtr yo'q: backend
 * bunday so'rovga 400 qaytaradi, shu sabab u umuman yuborilmaydi.
 */
function normalizeAgeRange(ageMin?: number, ageMax?: number): AgeRange | null {
  if (ageMin === undefined && ageMax === undefined) return null;
  if (ageMin !== undefined && ageMax !== undefined && ageMin > ageMax) return null;
  return {
    ...(ageMin === undefined ? {} : { ageMin }),
    ...(ageMax === undefined ? {} : { ageMax }),
  };
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
    gender: readEnum(searchParams, 'gender', GENDER_FILTER_VALUES),
    age: normalizeAgeRange(
      readAge(searchParams.get('ageMin')),
      readAge(searchParams.get('ageMax')),
    ),
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
    filters.gender !== '' ||
    filters.age !== null ||
    filters.needsAttention ||
    filters.from !== '' ||
    filters.to !== ''
  );
}
