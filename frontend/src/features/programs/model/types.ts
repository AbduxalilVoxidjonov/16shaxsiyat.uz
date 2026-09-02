import type { BadgeVariant } from '@/shared/ui/Badge';
import type { components } from '@/shared/api/schema';

/**
 * `AssessmentProgram` admin DTO'lari — `docs/07-api-shartnoma.md` 3.4-bo'limida yozilmagan
 * (hujjat P34/P35dan oldingi holatda), lekin backend (`AssessmentProgramsController`, P34)
 * haqiqiy va `schema.d.ts`da to'liq generatsiya qilingan — quyidagilar shu sxemadan
 * re-export (`docs/10`, 6-bo'lim qoidasi: "faqat generatsiya"). `prompts/34` E15-band +
 * `src/StudentRoadMap.Application/Admin/Programs/AdminProgramDtos.cs` — haqiqat manbai.
 */
export type AdminProgramListItem = components['schemas']['AdminProgramListItemDto'];
export type AdminProgramDetail = components['schemas']['AdminProgramDetailDto'];
export type AdminProgramTestItem = components['schemas']['AdminProgramTestItemDto'];
export type CreateProgramRequestBody = components['schemas']['CreateProgramRequest'];
export type UpdateProgramRequestBody = components['schemas']['UpdateProgramRequest'];
export type AddProgramTestRequestBody = components['schemas']['AddProgramTestRequest'];
export type ReorderProgramTestsRequestBody = components['schemas']['ReorderProgramTestsRequest'];

/**
 * Backend enum'larni JSON'da **string** qilib qaytaradi (docs/07 4-bo'lim), lekin Swagger
 * sxemasida oddiy `string` sifatida chiqadi (`ToString()` — literal union emas) — xuddi
 * `features/students/model/enums.ts`dagi naqsh: qiymatlar `docs/04-domain-model.md`/
 * `AssessmentProgram.cs` bilan qo'lda sinxronlanadi.
 */
export const PROGRAM_KIND_VALUES = ['System', 'Custom'] as const;
export type ProgramKind = (typeof PROGRAM_KIND_VALUES)[number];

export const PROGRAM_VISIBILITY_VALUES = ['Public', 'Assigned'] as const;
export type ProgramVisibility = (typeof PROGRAM_VISIBILITY_VALUES)[number];

export const PROGRAM_STATUS_VALUES = ['Draft', 'Published', 'Archived'] as const;
export type ProgramStatus = (typeof PROGRAM_STATUS_VALUES)[number];

export const PROGRAM_STATUS_BADGE_VARIANT: Record<ProgramStatus, BadgeVariant> = {
  Draft: 'neutral',
  Published: 'success',
  Archived: 'danger',
};

/** `GET /api/admin/programs` so'rov parametrlari. */
export interface ProgramsListQuery {
  search?: string;
  status?: ProgramStatus;
  isActive?: boolean;
  page: number;
  pageSize: number;
  sort?: string;
}

/**
 * Dastur "ilmiy batareya" (BIG5 + ACTIVITY) tekshiruvi uchun kerakli test kodlari —
 * `docs/06` §8 (2026-09-02, "Batareya majburiy emas") qarori: `MaturityIndex` faqat BIG5
 * **va** ACTIVITY ikkalasi birga bo'lganda hisoblanadi. Kodlar seed sxemasidan
 * (`prompts/04`, `test-definitions/*.json` `code` maydoni) qattiq yozilgan — dastur
 * tarkibidagi test kodlarini shu ro'yxat bilan solishtirib "batareya to'liqmi" aniqlanadi.
 */
export const MATURITY_BATTERY_TEST_CODES = ['BIG5', 'ACTIVITY'] as const;

/** Dastur nashr qilinganda "juda uzun" ogohlantirishi chegarasi — `prompts/35` 12-band. */
export const PROGRAM_DURATION_WARNING_MINUTES = 40;
