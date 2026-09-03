import type { components } from '@/shared/api/schema';
import type { ActivityLevel, AssessmentStatus, ReliabilityFlag } from './enums';

/**
 * O'quvchilar admin DTO'lari — `docs/07-api-shartnoma.md` 3.2-bo'lim.
 *
 * Backend (P14, `StudentsController`) tayyor va sxemada bor, shu sabab shakl
 * `shared/api/schema.d.ts` dan **re-export** (`docs/10` 6-bo'lim qoidasi).
 *
 * Enum maydonlari (`lastAssessmentStatus`, `activityLevel`, `reliabilityFlag`) sxemada
 * oddiy `string` — backend ularni `ToString()` bilan qaytaradi va Swashbuckle literal
 * union chiqara olmaydi. Shu sabab ular `Omit` + qayta e'lon bilan `enums.ts` dagi
 * torroq tiplarga TORAYTIRILADI: shunda `Record<AssessmentStatus, …>` jadvallari
 * (`ASSESSMENT_STATUS_BADGE_VARIANT`) to'liq qoladi va noma'lum qiymat `tsc` da ushlanadi.
 * Maydon NOMI yoki boshqa maydonlarning tipi o'zgarsa — sxema re-export'i qizaradi.
 */

/** `GET /api/admin/students` ro'yxat qatori — backend `AdminStudentListItemDto`. */
export type StudentListItemDto = Omit<
  components['schemas']['AdminStudentListItemDto'],
  'lastAssessmentStatus' | 'activityLevel' | 'reliabilityFlag'
> & {
  lastAssessmentStatus?: AssessmentStatus | null;
  activityLevel?: ActivityLevel | null;
  reliabilityFlag?: ReliabilityFlag | null;
};

/** `GET /api/admin/students` so'rov parametrlari — `docs/07` 3.2 (DTO emas, query shakli). */
export interface StudentsListQuery {
  schoolId?: string;
  grade?: number;
  status?: AssessmentStatus;
  needsAttention?: boolean;
  personalityType?: string;
  activityLevel?: ActivityLevel;
  from?: string;
  to?: string;
  search?: string;
  page: number;
  pageSize: number;
  sort?: string;
}
