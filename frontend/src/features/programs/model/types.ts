import type { components } from '@/shared/api/schema';
import {
  PROGRAM_STATE_VALUES,
  isProgramState,
  programStateBadgeVariant,
  programStateLabelKey,
  type ProgramState,
} from '@/shared/lib/programState';
import {
  DEFAULT_REGISTRATION_FIELDS,
  REGISTRATION_FIELD_KEYS,
  REGISTRATION_FIELD_MODE_VALUES,
  REGISTRATION_MODE_VALUES,
  resolveRegistrationFields,
  type AdminProgramDetailWithRegistration,
  type AdminProgramListItemWithRegistration,
  type CreateProgramRequestWithRegistration,
  type RegistrationFieldKey,
  type RegistrationFieldMode,
  type RegistrationFields,
  type RegistrationMode,
  type UpdateProgramRequestWithRegistration,
} from '@/shared/api/registrationModeTypes';

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
 * `registrationMode` (P52, 2026-09-11) — MUVAQQAT, `schema.d.ts`da hali yo'q
 * (`shared/api/registrationModeTypes.ts`dagi izohga qarang, `docs/07` §3.5). `AdminProgram
 * List/DetailWithRegistration` — ro'yxat/detal DTO'lari + shu maydon; `Create/
 * UpdateProgramRequestWithRegistration` — yaratish/tahrirlash so'rov tanasi + shu maydon.
 * Backend generatsiya qilingach bular olib tashlanadi, yuqoridagi `AdminProgramListItem`/
 * `AdminProgramDetail`/`CreateProgramRequestBody`/`UpdateProgramRequestBody` ularning o'rnini
 * bosadi (maydon o'shanda ular ichida allaqachon bo'ladi).
 */
export {
  DEFAULT_REGISTRATION_FIELDS,
  REGISTRATION_FIELD_KEYS,
  REGISTRATION_FIELD_MODE_VALUES,
  REGISTRATION_MODE_VALUES,
  resolveRegistrationFields,
};
export type {
  AdminProgramDetailWithRegistration,
  AdminProgramListItemWithRegistration,
  CreateProgramRequestWithRegistration,
  RegistrationFieldKey,
  RegistrationFieldMode,
  RegistrationFields,
  RegistrationMode,
  UpdateProgramRequestWithRegistration,
};

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

/**
 * Dasturning YAGONA holati — ta'rifi `@/shared/lib/programState` da (uni
 * `features/public-space` ham ishlatadi; feature'lar bir-birini import qilmaydi,
 * `docs/10` §2). Bu yerda faqat qayta eksport, feature ichidagi importlar qisqa bo'lsin.
 *
 * **2026-09-06:** ilgari shu faylda `PROGRAM_STATUS_VALUES`
 * (`Draft`/`Published`/`Archived`) bor edi va UI unga QO'SHIMCHA `isActive` belgisini
 * ko'rsatardi — natijada bitta dastur bir vaqtda "Arxiv" ham, "Faol" ham bo'lib ko'rinardi
 * (egasining topilmasi). Endi holat BITTA va u BACKENDDA hisoblanadi: klient
 * `status`/`isActive` juftligini ko'rmaydi ham, qayta talqin ham qilmaydi.
 */
export { PROGRAM_STATE_VALUES, isProgramState, programStateBadgeVariant, programStateLabelKey };
export type { ProgramState };

/** `GET /api/admin/programs` so'rov parametrlari. */
export interface ProgramsListQuery {
  search?: string;
  state?: ProgramState;
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
