/**
 * MUVAQQAT QO'LDA YOZILGAN TIPLAR — `docs/18-tarmoqlanuvchi-sorovnoma.md` §9
 * (`AssessmentProgram.RegistrationMode`, P52, 2026-09-11), `docs/07-api-shartnoma.md`
 * 1.1/1.2/3.5-bo'limlar.
 *
 * Backend allaqachon TAYYOR va commit qilingan (`AssessmentProgram.RegistrationMode`,
 * `GetSchoolInfoResult.programs[].registrationMode`/`.tests`, `StartSessionCommand`ning
 * NULLABLE shaxs maydonlari, `AdminProgramListItemDto`/`AdminProgramDetailDto`.
 * `registrationMode`, `Create`/`UpdateProgramRequest.registrationMode`), lekin `npm run
 * generate:api` bu sessiyada ISHGA TUSHIRILMAGAN (API ko'tarilmagan) — shu sabab
 * `schema.d.ts` hali eski holatda. Naqsh `shared/api/branchingTypes.ts`dagi bilan AYNAN bir
 * xil: backend chiqib generatsiya ishga tushgach bu fayl o'chiriladi, ishlatuvchi joylar
 * (`useSchoolInfo`, `useStartSession`, `features/programs/model/types.ts`) generatsiya
 * qilingan tiplarga qaytariladi.
 */
import type { components } from './schema';
import type { PublicSchoolInfo, PublicTestCatalogItem, StartSessionRequestBody } from './types';

/**
 * Admin dastur DTO'lari — `shared/` `features/*`ni import QILMAYDI (`docs/10` §2 "features
 * bir-birini import qilmaydi, umumiy narsa shared'ga chiqadi" qoidasining teskarisi ham rost:
 * shared ham feature'ga qaram bo'lmaydi). Shu sabab bu yerda ham `features/programs/model/
 * types.ts`dagi kabi to'g'ridan-to'g'ri sxemadan olinadi — feature keyin shu yerdan qayta
 * eksport qiladi (`AdminProgramListItem`/`AdminProgramDetail`bilan bir xil manba).
 */
type AdminProgramListItem = components['schemas']['AdminProgramListItemDto'];
type AdminProgramDetail = components['schemas']['AdminProgramDetailDto'];
type CreateProgramRequestBody = components['schemas']['CreateProgramRequest'];
type UpdateProgramRequestBody = components['schemas']['UpdateProgramRequest'];

/** `docs/18` §9 — `AssessmentProgram.RegistrationMode`. */
export const REGISTRATION_MODE_VALUES = ['Full', 'None'] as const;
export type RegistrationMode = (typeof REGISTRATION_MODE_VALUES)[number];

/**
 * ============================================================================
 * OMMAVIY QATLAM — `docs/07` §1.1/1.2.
 * ============================================================================
 */

/**
 * `GetSchoolInfoResult.programs[]` elementiga qo'shiladigan yangi maydonlar — `registrationMode`
 * va AYNAN shu dasturning `tests[]` (jonli hodisadan keyin tuzatilgan, `docs/07` §1.1 izohi:
 * yuqori darajadagi `tests[]` endi FAQAT mavjud dasturlar asosida hisoblanadi, bitta dasturli
 * tarmoqda esa `programs[0].tests` ishlatilishi kerak — arxivlangan dastur testlari sizib
 * chiqmasin).
 */
export interface PublicProgramRegistrationFields {
  registrationMode: RegistrationMode;
  tests: PublicTestCatalogItem[];
}

/** `PublicProgramSummaryDto` (generatsiya qilingan) + yuqoridagi yangi maydonlar. */
export type PublicProgramWithRegistration = PublicSchoolInfo['programs'][number] &
  PublicProgramRegistrationFields;

/**
 * `GetSchoolInfoResult` (generatsiya qilingan) + `programs[]` ning kengaytirilgan shakli.
 * `useSchoolInfo` shu tipni qaytaradi — `LandingPage`/`RegistrationPage` ishlatadi.
 */
export type PublicSchoolInfoWithRegistration = Omit<PublicSchoolInfo, 'programs'> & {
  programs: PublicProgramWithRegistration[];
};

/**
 * `POST /api/public/sessions` — `registrationMode: "None"` dasturdagi ANONIM so'rov tanasi
 * (`docs/07` §1.2 "Anonim oqim"). Shaxs maydonlari UMUMAN YO'Q (yuborilsa ham backend
 * e'tiborsiz qoldiradi — bu yerda ular hatto ixtiyoriy ham emas, chunki `LandingPage` ularni
 * hech qachon to'ldirmaydi).
 */
export interface StartSessionAnonymousRequestBody {
  slug: string;
  accessToken: string;
  accessCode?: string;
  consentAccepted: boolean;
  languageCode?: string;
  programCode?: string;
}

/**
 * `useStartSession` mutatsiyasi qabul qiladigan ikkala shakl — `registrationMode: "Full"`
 * (mavjud `StartSessionRequestBody`, `RegistrationPage`) yoki `"None"` (yuqoridagi anonim
 * shakl, `LandingPage`).
 */
export type StartSessionPayload = StartSessionRequestBody | StartSessionAnonymousRequestBody;

/**
 * ============================================================================
 * ADMIN QATLAMI — `docs/07` §3.5 ("Dastur `registrationMode` — admin CRUD").
 * ============================================================================
 */

/**
 * `AdminProgramListItemDto`/`AdminProgramDetailDto`ga qo'shiladigan yangi maydonlar.
 * `hasPersonalityBattery` (P52, 2026-09-11, `docs/07` §3.5 "Dastur `hasPersonalityBattery` —
 * admin ham") — backend `Domain.Catalog.PersonalityBattery` qoidasidan hisoblab beradi
 * (`Kind == Standard && ScoringMode == Scored`), N+1 yo'q batch so'rov bilan. Frontend endi
 * kod ro'yxati (`"MBTI16"`/`"BIG5"`/...) bilan TAXMIN QILMAYDI — shu bayroqqa ishonadi
 * (`features/programs/model/programComputations.ts`dagi eski `PERSONALITY_BATTERY_TEST_CODES`
 * o'chirildi).
 */
export interface AdminProgramRegistrationModeFields {
  registrationMode: RegistrationMode;
  hasPersonalityBattery: boolean;
}

/** `AdminProgramListItemDto` (generatsiya qilingan) + `registrationMode`. */
export type AdminProgramListItemWithRegistration = AdminProgramListItem &
  AdminProgramRegistrationModeFields;

/** `AdminProgramDetailDto` (generatsiya qilingan) + `registrationMode`. */
export type AdminProgramDetailWithRegistration = AdminProgramDetail &
  AdminProgramRegistrationModeFields;

/**
 * `POST`/`PUT /api/admin/programs` so'rov tanasiga qo'shiladigan ixtiyoriy maydon — standart
 * `"Full"` (`docs/07` §3.5: "ixtiyoriy `registrationMode`, standart `"Full"`").
 */
export interface AdminProgramRegistrationModePayload {
  registrationMode?: RegistrationMode;
}

export type CreateProgramRequestWithRegistration = CreateProgramRequestBody &
  AdminProgramRegistrationModePayload;

export type UpdateProgramRequestWithRegistration = UpdateProgramRequestBody &
  AdminProgramRegistrationModePayload;
