import type { ActivityLevel, AssessmentStatus, ReliabilityFlag } from './enums';

/**
 * O'quvchilar admin DTO'lari — docs/07-api-shartnoma.md, 3.2-bo'lim.
 *
 * // TODO: P14 backend endpointlari swagger'ga tushgach (`npm run generate:api` — P24
 * // topshirig'ida ataylab ISHGA TUSHIRILMAGAN, chunki P15 endpointlari hali sxemaga
 * // tushmagan va chala kontrakt olinardi) quyidagi qo'lda yozilgan tiplar `schema.d.ts`dan
 * // re-export bilan almashtiriladi (`docs/10`, 6-bo'lim; xuddi `features/schools/model/types.ts`
 * // dagi TODO bilan bir xil naqsh, P23 hisoboti).
 */

/** `StudentListItemDto` — docs/07 3.2. */
export interface StudentListItemDto {
  id: string;
  fullName: string;
  schoolName: string;
  grade: number;
  classLetter: string | null;
  phone: string;
  lastAssessmentStatus: AssessmentStatus | null;
  personalityType: string | null;
  maturityIndex: number | null;
  activityLevel: ActivityLevel | null;
  needsAttention: boolean;
  reliabilityFlag: ReliabilityFlag | null;
  lastAssessmentAt: string | null;
}

/** `GET /api/admin/students` so'rov parametrlari — docs/07 3.2. */
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
