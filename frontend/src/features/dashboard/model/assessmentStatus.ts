import type { BadgeVariant } from '@/shared/ui/Badge';

/**
 * Sessiya holati belgisi (`RecentAssessmentsList`) uchun rang xaritasi —
 * `features/students/model/enums.ts`dagi `ASSESSMENT_STATUS_BADGE_VARIANT` bilan atayin
 * bir xil qiymatlar, lekin **mustaqil nusxa** (docs/10, 2-bo'lim: "features/* bir-birini
 * import qilmaydi — umumiy narsa shared/ yoki widgets/ga chiqadi"; bu ikkita alohida
 * feature'ning ichki modeli, umumiy joyga chiqarish uchun hali uchinchi ishlatuvchi yo'q).
 * Noma'lum holat kelsa (`docs/05` enumiga yangi qiymat qo'shilsa) `neutral`ga tushadi —
 * sahifa yiqilmaydi.
 */
export const RECENT_ASSESSMENT_STATUS_BADGE_VARIANT: Record<string, BadgeVariant> = {
  Draft: 'neutral',
  InProgress: 'primary',
  Completed: 'primary',
  Analyzing: 'warning',
  Analyzed: 'success',
  AnalysisFailed: 'danger',
  Abandoned: 'neutral',
};
