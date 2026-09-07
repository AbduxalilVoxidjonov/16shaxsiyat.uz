import { ASSESSMENT_STATUS_VALUES, type AssessmentStatus } from './types';

/** URL'da saqlanadigan filtr kalitlari (jadval holati — `useServerTableState` da alohida). */
export const ASSESSMENTS_FILTER_KEYS = ['status', 'schoolId', 'from', 'to'] as const;
export type AssessmentsFilterKey = (typeof ASSESSMENTS_FILTER_KEYS)[number];

export interface AssessmentsFilterValues {
  status: AssessmentStatus | '';
  schoolId: string;
  /** `YYYY-MM-DD` yoki `''`. */
  from: string;
  to: string;
}

const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;

/**
 * URL query'dan filtrlarni o'qiydi — `readStudentsFilters` (P24) bilan bir xil naqsh.
 *
 * Boshqaruv panelidagi "Tahlil navbatida" kartasi `/admin/assessments?status=Analyzing`
 * manziliga o'tadi (`features/dashboard/components/KpiCards.tsx`), shu sabab `status`
 * URL'dan O'QILADI va birinchi renderdayoq qo'llanadi — alohida "filtrni qo'llash" qadami
 * yo'q. Noma'lum qiymat jimgina tashlanadi ("hammasi" bo'lib qoladi).
 */
export function readAssessmentsFilters(searchParams: URLSearchParams): AssessmentsFilterValues {
  const status = searchParams.get('status');
  const from = searchParams.get('from') ?? '';
  const to = searchParams.get('to') ?? '';
  return {
    status:
      status && (ASSESSMENT_STATUS_VALUES as readonly string[]).includes(status)
        ? (status as AssessmentStatus)
        : '',
    schoolId: searchParams.get('schoolId') ?? '',
    from: ISO_DATE.test(from) ? from : '',
    to: ISO_DATE.test(to) ? to : '',
  };
}

export function hasActiveAssessmentsFilters(filters: AssessmentsFilterValues): boolean {
  return (
    filters.status !== '' ||
    filters.schoolId !== '' ||
    filters.from !== '' ||
    filters.to !== ''
  );
}
