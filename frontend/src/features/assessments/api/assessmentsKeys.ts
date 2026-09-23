/**
 * `features/assessments` uchun TanStack Query kalitlari — `docs/10`, 5.2-bo'lim konvensiyasi.
 * Ro'yxat (`list`) va maktab tanlovi (`schoolOptions`) kalitlari 2026-09-23 da sessiyalar
 * ro'yxati sahifasi bilan birga olib tashlandi — bu feature endi faqat sessiya detalini beradi.
 */
export const ASSESSMENTS_QUERY_KEYS = {
  detail: (assessmentId: string) => ['assessments', 'detail', assessmentId] as const,
};
