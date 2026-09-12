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
import type { Gender, PublicSchoolInfo, PublicTestCatalogItem } from './types';
import type {
  RegistrationCustomFieldAnswers,
  RegistrationFormDefinition,
} from './registrationFormSettingsTypes';

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
 * `docs/18` §9 kengaytmasi (P52, 2026-09-11) — `RegistrationMode = "Full"` bo'lsa, HAR BIR
 * shaxs maydoni (F.I.Sh.dan tashqari — u har doim majburiy) alohida "Yashirin"/"Ixtiyoriy"/
 * "Majburiy" qilib sozlanadi. Backend `GET /api/public/schools/{slug}`ning
 * `programs[].registrationFields`sida va admin `Create/UpdateProgramRequest`/
 * `AdminProgramDetailDto`da shu shaklda qaytadi. Qattiq qoida: shaxsiyat batareyasi bor
 * dasturda `birthDate`/`grade` DOIM `"Required"` — ball normalari shularga tayanadi
 * (buzilsa `400 REGISTRATION_FIELD_REQUIRED_FOR_BATTERY`).
 */
export const REGISTRATION_FIELD_MODE_VALUES = ['Hidden', 'Optional', 'Required'] as const;
export type RegistrationFieldMode = (typeof REGISTRATION_FIELD_MODE_VALUES)[number];

/** `fullName` bu ro'yxatda YO'Q — `RegistrationMode = Full` bo'lsa har doim majburiy. */
export const REGISTRATION_FIELD_KEYS = [
  'birthDate',
  'gender',
  'grade',
  'classLetter',
  'phone',
  'parentPhone',
  'email',
] as const;
export type RegistrationFieldKey = (typeof REGISTRATION_FIELD_KEYS)[number];

export type RegistrationFields = Record<RegistrationFieldKey, RegistrationFieldMode>;

/**
 * Standart sozlama — hozirgi (P52dan oldingi) qattiq yozilgan xatti-harakat bilan AYNAN mos:
 * F.I.Sh./tug'ilgan sana/jins/sinf/telefon majburiy, sinf harfi/ota-ona telefoni/email
 * ixtiyoriy. `registrationFields` javobda kelmasa (eski test fixture'lari, hali yangilanmagan
 * backend javoblari) shu qiymatlar bilan to'ldiriladi — regressiya qulfi.
 */
export const DEFAULT_REGISTRATION_FIELDS: RegistrationFields = {
  birthDate: 'Required',
  gender: 'Required',
  grade: 'Required',
  classLetter: 'Optional',
  phone: 'Required',
  parentPhone: 'Optional',
  email: 'Optional',
};

/** `registrationFields` qisman yoki umuman kelmagan javoblarni standart bilan to'ldiradi. */
export function resolveRegistrationFields(
  fields?: Partial<RegistrationFields> | null,
): RegistrationFields {
  return { ...DEFAULT_REGISTRATION_FIELDS, ...fields };
}

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
  /**
   * P52 kengaytmasi — har bir shaxs maydonining "Yashirin"/"Ixtiyoriy"/"Majburiy" holati.
   * IXTIYORIY tip darajasida: eski javoblarda (hali yangilanmagan backend/eski test
   * fixture, masalan `LandingPage.test.tsx`/`RegistrationPage.test.tsx`dagi ko'plab mavjud
   * o'rnaklar) bu maydon umuman bo'lmasligi mumkin — o'qiydigan joy
   * `resolveRegistrationFields()` bilan standart qiymatlarga to'ldiradi (regressiya qulfi,
   * `registrationMode`/`hasPersonalityBattery`dan farqli — ular har doim majburiy edi,
   * chunki mavjud testlar ularni allaqachon har joyda aniq bergan).
   */
  registrationFields?: RegistrationFields;
  /**
   * P52 2-to'lqin (2026-09-12, `docs/18` §9.6.2) — TO'LIQ GLOBAL ro'yxatdan o'tish formasi
   * ta'rifi (superadmin qo'shgan `customFields[]` bilan birga), dastur ustunligi (batareya
   * invarianti) QO'LLANGAN holda. `registrationFields` shu obyektning `coreFields`ga mos
   * qisqartirilgan proyeksiyasi — ikkalasi BIR XIL manbadan hisoblanadi. Ixtiyoriy — eski
   * javoblarda (hali yangilanmagan fixture) bo'lmasligi mumkin, `RegistrationPage`
   * `REGISTRATION_FORM_DEFAULT_DEFINITION` bilan to'ldiradi (regressiya qulfi).
   */
  registrationForm?: RegistrationFormDefinition;
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
 * `POST /api/public/sessions` — `registrationMode: "Full"` dasturdagi so'rov tanasi, P52
 * kengaytmasi bilan: har bir shaxs maydoni endi dasturning `registrationFields`iga qarab
 * **ixtiyoriy** (avvalgi `StartSessionRequestBody`, generatsiya qilingan `schema.d.ts`,
 * ularni doim majburiy deb belgilagan edi — bu yerdagi qo'lda yozilgan nusxa haqiqiy
 * shartnomaga mos, backend generatsiya qilingach olib tashlanadi). `fullName` bundan
 * mustasno — u har doim majburiy (`docs/18` §9 "`fullName` sozlamada yo'q").
 */
export interface StartSessionRegistrationRequestBody {
  slug: string;
  accessToken: string;
  accessCode?: string;
  fullName: string;
  birthDate?: string;
  gender?: Gender;
  grade?: number;
  classLetter?: string;
  phone?: string;
  parentPhone?: string;
  email?: string;
  consentAccepted: boolean;
  languageCode?: string;
  programCode?: string;
  /**
   * P52 2-to'lqin (2026-09-12, `docs/18` §9.6.2) — superadmin qo'shgan "o'z maydonlari"
   * javoblari, kod → qiymat. Ixtiyoriy — hech qanday o'z maydon bo'lmasa umuman yuborilmaydi.
   */
  customFields?: RegistrationCustomFieldAnswers;
}

/**
 * `useStartSession` mutatsiyasi qabul qiladigan ikkala shakl — `registrationMode: "Full"`
 * (yuqoridagi `StartSessionRegistrationRequestBody`, `RegistrationPage`) yoki `"None"`
 * (anonim shakl, `LandingPage`).
 */
export type StartSessionPayload = StartSessionRegistrationRequestBody | StartSessionAnonymousRequestBody;

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
  /**
   * P52 kengaytmasi — `docs/18` §9, faqat `AdminProgramDetailDto`da mazmunli. Ixtiyoriy —
   * `ProgramFormDialog` `resolveRegistrationFields()` bilan standartga to'ldiradi (mavjud
   * `ProgramDetailPage.test.tsx` o'rnaklari bu maydonsiz ham to'g'ri kompilyatsiya bo'lsin).
   */
  registrationFields?: RegistrationFields;
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
  /** Standart — `DEFAULT_REGISTRATION_FIELDS` (`docs/18` §9). */
  registrationFields?: RegistrationFields;
}

export type CreateProgramRequestWithRegistration = CreateProgramRequestBody &
  AdminProgramRegistrationModePayload;

export type UpdateProgramRequestWithRegistration = UpdateProgramRequestBody &
  AdminProgramRegistrationModePayload;
