import type { BadgeVariant } from '@/shared/ui/Badge';

/**
 * O'quvchi ro'yxati DTO'sidagi enum qiymatlari — docs/05-database-schema.md (raqamli enum
 * jadvali) va docs/04-domain-model.md 2.3-bo'limi bilan bir xil string nomlar (backend
 * enum'larni JSON'da **string** qilib qaytaradi — docs/07, 4-bo'lim "Umumiy konvensiyalar").
 */

/** `docs/05`: "AssessmentStatus | 0 Draft, 1 InProgress, 2 Completed, 3 Analyzing, 4 Analyzed, 5 AnalysisFailed, 6 Abandoned". */
export const ASSESSMENT_STATUS_VALUES = [
  'Draft',
  'InProgress',
  'Completed',
  'Analyzing',
  'Analyzed',
  'AnalysisFailed',
  'Abandoned',
] as const;
export type AssessmentStatus = (typeof ASSESSMENT_STATUS_VALUES)[number];

/** `docs/05`: "ReliabilityFlag | 1 Reliable, 2 Questionable, 3 Unreliable". */
export const RELIABILITY_FLAG_VALUES = ['Reliable', 'Questionable', 'Unreliable'] as const;
export type ReliabilityFlag = (typeof RELIABILITY_FLAG_VALUES)[number];

/**
 * Jins filtri qiymatlari — backend `?gender=` faqat `Male`/`Female` qabul qiladi
 * (`Unspecified` → 400, `docs/07` 3.2). Domain enum'i `docs/05` 3-bo'lim: "Gender | 0 Unspecified,
 * 1 Male, 2 Female" — filtrda `Unspecified` yo'q, chunki "jinsi ko'rsatilmaganlar" kesimi
 * admin uchun ma'nosiz.
 */
export const GENDER_FILTER_VALUES = ['Male', 'Female'] as const;
export type GenderFilter = (typeof GENDER_FILTER_VALUES)[number];

/** `docs/05`: "ActivityLevel | 1 Passive, 2 LowActive, 3 Moderate, 4 Active, 5 HighlyActive". */
export const ACTIVITY_LEVEL_VALUES = [
  'Passive',
  'LowActive',
  'Moderate',
  'Active',
  'HighlyActive',
] as const;
export type ActivityLevel = (typeof ACTIVITY_LEVEL_VALUES)[number];

/**
 * 16 tipli shaxsiyat modeli kodlari (`docs/03`, 2-bo'lim — Yung tipologiyasi 4 dixotomiyasi:
 * `EI`, `SN`, `TF`, `JP`). O'zbekcha nomlar (`TypeCatalog.NameUz`, masalan INTJ — "Loyihachi")
 * bu loyihada mustaqil tanlangan va faqat backend/`TypeCatalog` jadvalida seed qilingan
 * (`docs/03` 112-qator, `docs/17` 77-qator) — frontendda hali ularni olib keladigan ommaviy
 * katalog endpointi ulanmagan (faqat admin `GET /api/admin/catalog/type-catalog`, docs/07
 * 3.4, u ham hozircha boshqa promptda). Shu sabab filtr va jadvalda **kod** ko'rsatiladi
 * (`INTJ`), xuddi `docs/11` A-5 wireframe'idagi kabi ("INTJ — Loyihachi" o'rniga hozircha
 * faqat kod) — PM/backend tasdiqlasa keyingi promptda `TypeCatalog` bilan almashtiriladi.
 */
export const PERSONALITY_TYPE_CODES = [
  'INTJ',
  'INTP',
  'ENTJ',
  'ENTP',
  'INFJ',
  'INFP',
  'ENFJ',
  'ENFP',
  'ISTJ',
  'ISFJ',
  'ESTJ',
  'ESFJ',
  'ISTP',
  'ISFP',
  'ESTP',
  'ESFP',
] as const;

export const ASSESSMENT_STATUS_BADGE_VARIANT: Record<AssessmentStatus, BadgeVariant> = {
  Draft: 'neutral',
  InProgress: 'primary',
  Completed: 'primary',
  Analyzing: 'warning',
  Analyzed: 'success',
  AnalysisFailed: 'danger',
  Abandoned: 'neutral',
};

export const RELIABILITY_FLAG_BADGE_VARIANT: Record<ReliabilityFlag, BadgeVariant> = {
  Reliable: 'success',
  Questionable: 'warning',
  Unreliable: 'danger',
};
