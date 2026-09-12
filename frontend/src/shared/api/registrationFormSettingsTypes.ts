/**
 * MUVAQQAT QO'LDA YOZILGAN TIPLAR — `docs/07-api-shartnoma.md` §3.8,
 * `docs/18-tarmoqlanuvchi-sorovnoma.md` §9.6 (2026-09-11/12, egasining talabi: GLOBAL
 * ro'yxatdan o'tish formasi sozlamasi — "Sozlamalar" sahifasidan boshqariladi).
 *
 * Backend TO'LIQ TAYYOR va commit qilingan (`SettingsController.GetRegistrationForm`/
 * `UpdateRegistrationForm`, `RegistrationFormDefinitionDto` va h.k.), lekin bu sessiyada
 * `npm run generate:api` ISHGA TUSHIRILMAGAN (frontend vazifasi backend'dan alohida
 * to'lqin sifatida yuklandi) — shu sabab `schema.d.ts`da bu DTO'lar hali yo'q. Naqsh
 * `shared/api/branchingTypes.ts`/`shared/api/registrationModeTypes.ts` bilan AYNAN bir xil:
 * backend generatsiya ishga tushgach bu fayl o'chiriladi, ishlatuvchi joylar
 * (`features/settings/**`) generatsiya qilingan tiplarga o'tkaziladi.
 *
 * Shakl `docs/07` §3.8 / `docs/05` (jsonb) bilan bayt-bayt bir xil. Nomlash ataylab
 * backend DTO nomlaridan (`RegistrationFormDefinitionDto`, `RegistrationCoreFieldDto`, ...)
 * FARQ QILADI — `eslint.config.js`dagi "qo'lda DTO yozilmasin" qoidasi `shared/api/**`da
 * `*Dto`/`*Request`/`*Response`/`*Result`/`*Item`/`*Detail` bilan tugagan nomlarni
 * taqiqlaydi (`docs/10` §6.3).
 */
import type { RegistrationFieldMode } from './registrationModeTypes';

/**
 * `docs/06` §6: `Hidden`/`Optional`/`Required`. Eski `AssessmentProgram.RegistrationFields`
 * (§9.5, `registrationModeTypes.ts`) bilan BIR XIL uch qiymat — shu sabab tur qayta
 * ishlatiladi (ikkinchi mustaqil enum ro'yxati paydo bo'lmasin).
 */
export type RegistrationFormFieldRequirement = RegistrationFieldMode;
export const REGISTRATION_FORM_FIELD_REQUIREMENT_VALUES = ['Hidden', 'Optional', 'Required'] as const;

/** `docs/18` §9.6.1 — `RegistrationCustomField.Type`, mavjud `QuestionType` nomlaridan. */
export const REGISTRATION_FORM_CUSTOM_FIELD_TYPES = [
  'ShortText',
  'LongText',
  'Phone',
  'SingleChoice',
  'MultiChoice',
] as const;
export type RegistrationFormCustomFieldType = (typeof REGISTRATION_FORM_CUSTOM_FIELD_TYPES)[number];

/** `ShortText`/`Phone` — `inputPattern` qo'llab-quvvatlanadi, `LongText` — yo'q (docs/07 §3.8). */
export const REGISTRATION_FORM_PATTERNABLE_TYPES: readonly RegistrationFormCustomFieldType[] = [
  'ShortText',
  'Phone',
];

/** Matn asosidagi turlar — `maxLength` qo'llaniladi. */
export const REGISTRATION_FORM_TEXT_TYPES: readonly RegistrationFormCustomFieldType[] = [
  'ShortText',
  'LongText',
  'Phone',
];

/** Tanlov asosidagi turlar — `options[]` majburiy (kamida 2 ta). */
export const REGISTRATION_FORM_CHOICE_TYPES: readonly RegistrationFormCustomFieldType[] = [
  'SingleChoice',
  'MultiChoice',
];

/** Sakkizta qattiq kodlangan asosiy maydonning kaliti — `RegistrationCoreFields.ReservedCodes`. */
export const REGISTRATION_FORM_CORE_FIELD_KEYS = [
  'fullName',
  'birthDate',
  'gender',
  'grade',
  'classLetter',
  'phone',
  'parentPhone',
  'email',
] as const;
export type RegistrationFormCoreFieldKey = (typeof REGISTRATION_FORM_CORE_FIELD_KEYS)[number];

/** `docs/07` §3.8 — bitta asosiy maydon sozlamasi. */
export interface RegistrationFormCoreField {
  requirement: RegistrationFormFieldRequirement;
  labelUz: string;
  placeholderUz: string | null;
  order: number;
}

export type RegistrationFormCoreFields = Record<RegistrationFormCoreFieldKey, RegistrationFormCoreField>;

/** `docs/07` §3.8 — `SingleChoice`/`MultiChoice` variant elementi (`value` — ERKIN matn, son EMAS). */
export interface RegistrationFormCustomFieldOption {
  textUz: string;
  value: string;
  order: number;
}

/** `docs/07` §3.8 — superadmin qo'shgan o'z maydoni. */
export interface RegistrationFormCustomField {
  code: string;
  type: RegistrationFormCustomFieldType;
  labelUz: string;
  placeholderUz: string | null;
  requirement: RegistrationFormFieldRequirement;
  maxLength: number | null;
  inputPattern: string | null;
  options: RegistrationFormCustomFieldOption[] | null;
  order: number;
}

/** `GET`/`PUT /api/admin/settings/registration-form` — ikkalasi ham BIR XIL shakl. */
export interface RegistrationFormDefinition {
  coreFields: RegistrationFormCoreFields;
  customFields: RegistrationFormCustomField[];
}

/**
 * `RegistrationFormDefinition.Default` (Domain) bilan BAYT-BAYT bir xil — sozlama umuman
 * yaratilmagan bo'lganda `GET` shu qiymatni qaytaradi (`docs/07` §3.8: "sozlama yo'q bo'lsa
 * STANDART qaytadi").
 */
export const REGISTRATION_FORM_DEFAULT_DEFINITION: RegistrationFormDefinition = {
  coreFields: {
    fullName: { requirement: 'Required', labelUz: 'F.I.Sh.', placeholderUz: null, order: 1 },
    birthDate: { requirement: 'Required', labelUz: "Tug'ilgan sana", placeholderUz: null, order: 2 },
    gender: { requirement: 'Required', labelUz: 'Jins', placeholderUz: null, order: 3 },
    grade: { requirement: 'Required', labelUz: 'Sinf', placeholderUz: null, order: 4 },
    classLetter: { requirement: 'Optional', labelUz: 'Sinf harfi', placeholderUz: null, order: 5 },
    phone: { requirement: 'Required', labelUz: 'Telefon raqami', placeholderUz: null, order: 6 },
    parentPhone: { requirement: 'Optional', labelUz: "Ota-ona telefoni", placeholderUz: null, order: 7 },
    email: { requirement: 'Optional', labelUz: 'Email', placeholderUz: null, order: 8 },
  },
  customFields: [],
};

/**
 * `docs/07` §1.2/§5.1b/§5.4 (P52 2-to'lqin, 2026-09-12) — `POST /api/public/sessions`/`PUT`/
 * `POST /api/me/profile`/`sessions` so'rov tanasidagi `customFields` qismi: kod → qiymat.
 * Matn turlarida (`ShortText`/`LongText`/`Phone`) satr, `SingleChoice`da variant `order`i
 * (butun son), `MultiChoice`da variant `order`lari massivi. `value` (matn) EMAS — `order`
 * barqaror, admin yorliq/matnni o'zgartirsa ham yuborilgan javob buzilmaydi.
 */
export type RegistrationCustomFieldAnswerValue = string | number | number[];
export type RegistrationCustomFieldAnswers = Record<string, RegistrationCustomFieldAnswerValue>;

/** `docs/06` §6 — `PUT /api/admin/settings/registration-form` xato kodlari. */
export const REGISTRATION_FORM_ERROR_CODES = {
  fullNameLocked: 'REGISTRATION_FORM_FULL_NAME_LOCKED',
  fieldCodeInvalid: 'REGISTRATION_FORM_FIELD_CODE_INVALID',
  fieldCodeDuplicate: 'REGISTRATION_FORM_FIELD_CODE_DUPLICATE',
  choiceOptionsInsufficient: 'REGISTRATION_FORM_CHOICE_OPTIONS_INSUFFICIENT',
  optionValueDuplicate: 'REGISTRATION_FORM_OPTION_VALUE_DUPLICATE',
  inputPatternInvalid: 'INPUT_PATTERN_INVALID',
} as const;
