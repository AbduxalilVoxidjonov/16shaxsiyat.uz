import { z } from 'zod';
import { calculateAge, isValidCalendarDate } from '@/shared/lib/birthDate';
import type {
  RegistrationFormCustomField,
  RegistrationFormFieldRequirement,
} from '@/shared/api/registrationFormSettingsTypes';
import { customFieldsRecordSchema, validateCustomFields } from '@/shared/lib/registrationFormFields';

/**
 * Ommaviy (maktabsiz) anketa validatsiyasi — `docs/07` §5.4.
 *
 * Maktab oqimidagi sxemadan (`features/public-assessment/schemas/registrationSchema.ts`)
 * ATAYLAB alohida, chunki qoidalar boshqacha:
 *
 * | Maydon | Maktab (§1.2) | Ommaviy (§5.4) |
 * |---|---|---|
 * | yosh | 6–20 | **6–99** |
 * | `grade` | majburiy 1–11 | **ixtiyoriy** (`null` = maktabda o'qimaydi) |
 * | `classLetter`/`parentPhone` | bor | **yo'q** |
 * | `parentalConsent` | yo'q | **18 yoshgacha `true` bo'lishi SHART** |
 * | `slug`/`accessToken`/`accessCode` | majburiy | **yo'q** (egalik JWT bilan) |
 *
 * Umumiy qismi (sana hisobi) `shared/lib/birthDate` da — nusxa ko'chirilmaydi.
 * Yakuniy haqiqat manbai baribir backend; bu yerdagi tekshiruv faqat tezroq va do'stona
 * xabar berish uchun.
 */
export const MIN_AGE = 6;
export const MAX_AGE = 99;
/** Shu yoshgacha ota-ona roziligi majburiy (`docs/07` §5.4). */
export const PARENTAL_CONSENT_AGE = 18;
const MIN_FULLNAME_LENGTH = 5;

const birthDateSchema = z.object({
  day: z.string().min(1, 'Kunni tanlang.'),
  month: z.string().min(1, 'Oyni tanlang.'),
  year: z.string().min(1, 'Yilni tanlang.'),
});

/**
 * To'ldirilgan sanadan yoshni hisoblaydi; sana to'liq/haqiqiy bo'lmasa `null`.
 * Forma `parentalConsent` maydonini shu qiymatga qarab ko'rsatadi.
 */
export function ageFromFormValue(
  value: { day: string; month: string; year: string },
  now: Date = new Date(),
): number | null {
  if (!value.day || !value.month || !value.year) return null;
  const day = Number(value.day);
  const month = Number(value.month);
  const year = Number(value.year);
  if (!isValidCalendarDate(day, month, year)) return null;
  return calculateAge(new Date(Date.UTC(year, month - 1, day)), now);
}

const GENDER_ERROR_MESSAGE = 'Jinsni tanlang.';

// `includes()` ataylab: to'g'ridan-to'g'ri tenglik tekshiruvi TS 5.5+ da chiqish tipini
// toraytirib, RHF `defaultValues` dagi bo'sh `''` bilan mos kelmay qolardi.
const genderRequiredSchema = z
  .string()
  .refine((value) => (['Male', 'Female'] as const).includes(value as 'Male' | 'Female'), {
    message: GENDER_ERROR_MESSAGE,
  });
const genderLenientSchema = z
  .string()
  .refine(
    (value) => value === '' || (['Male', 'Female'] as const).includes(value as 'Male' | 'Female'),
    { message: GENDER_ERROR_MESSAGE },
  );

/**
 * Sxema PROFIL HOLATIGA bog'liq (`lib/profileState.ts`): rozilik joriy bo'lsa (`ready`/`edit`
 * profil bilan) blok ko'rsatilmaydi va `consentAccepted` talab qilinmaydi — server ham shu
 * holatda talab qilmaydi (`docs/07` §5.4). Qolgan qoidalar (F.I.Sh., sana, telefon, 18
 * yoshgacha ota-ona roziligi) uchala holatda bir xil — mavjud profil tahririda ham forma
 * to'liq yuboriladi.
 *
 * `genderRequirement` — P52 2-to'lqin (2026-09-12, `docs/18` §9.6.2): GLOBAL
 * `registrationForm.coreFields.gender.requirement`ga ergashadi (standart `'Required'` —
 * P52 2-to'lqindan oldingi xatti-harakat bilan bir xil, regressiya qulfi). **Diqqat:**
 * `birthDate`/`phone` bu oqimda sozlamadan qat'i nazar HAMISHA majburiy qoladi — backend
 * (`PublicStudentProfile.RequireFields`) faqat `gender`ni GLOBAL sozlamaga qarab tekshiradi,
 * `birthDate`/`phone` har doim shart (2-to'lqin qamrovi shu ikkalasini hali qamramaydi).
 *
 * `customFields` — superadmin qo'shgan "o'z maydonlari"; `requireCustomFields` — FAQAT yangi
 * profil yaratilayotganda (`mode === 'new'`) `true` (`docs/07` §5.1b/§5.4: "faqat profil
 * YARATILAYOTGANDA so'raladi" — `PublicStudentProfile.ValidateCustomFields`
 * `requireMandatory: existing is null` bilan bir xil qoida).
 */
export interface PublicRegistrationSchemaOptions {
  /** `false` — rozilik joriy, checkbox ko'rsatilmaydi va tekshirilmaydi. */
  requireConsent: boolean;
  genderRequirement?: RegistrationFormFieldRequirement;
  customFields?: readonly RegistrationFormCustomField[];
  /** `true` — faqat yangi profil (`mode === 'new'`) yaratilayotganda. */
  requireCustomFields?: boolean;
}

export function createPublicRegistrationSchema({
  requireConsent,
  genderRequirement = 'Required',
  customFields = [],
  requireCustomFields = false,
}: PublicRegistrationSchemaOptions) {
  // Majburiylik faqat yangi profilda tekshiriladi (`requireCustomFields`) — aks holda
  // `Required` maydon ham "Optional" kabi ishlaydi (backend bilan bir xil, yuqoridagi izoh).
  const effectiveCustomFields = requireCustomFields
    ? customFields
    : customFields.map((field) =>
        field.requirement === 'Required' ? { ...field, requirement: 'Optional' as const } : field,
      );

  return baseSchema
    .extend({
      gender: genderRequirement === 'Required' ? genderRequiredSchema : genderLenientSchema,
      consentAccepted: requireConsent
        ? z
            .boolean()
            .refine((value) => value === true, { message: "Roziliksiz testni boshlab bo'lmaydi." })
        : z.boolean(),
      customFields: customFieldsRecordSchema,
    })
    .superRefine((values, ctx) => {
      refineBirthDateAndParentalConsent(values, ctx);
      validateCustomFields(effectiveCustomFields, values.customFields, ctx);
    });
}

function refineBirthDateAndParentalConsent(
  values: { birthDate: { day: string; month: string; year: string }; parentalConsent: boolean },
  ctx: z.RefinementCtx,
): void {
  const { day, month, year } = values.birthDate;
  if (!day || !month || !year) {
    return; // Bo'sh maydonlar allaqachon `min(1)` orqali xabar bergan.
  }

  if (!isValidCalendarDate(Number(day), Number(month), Number(year))) {
    ctx.addIssue({
      code: 'custom',
      message: 'Bunday sana mavjud emas.',
      path: ['birthDate', 'day'],
    });
    return;
  }

  const age = ageFromFormValue(values.birthDate);
  if (age === null) return;

  if (age < MIN_AGE || age > MAX_AGE) {
    ctx.addIssue({
      code: 'custom',
      message: `Tug'ilgan sana ${String(MIN_AGE)}-${String(MAX_AGE)} yosh oralig'iga to'g'ri kelishi kerak.`,
      path: ['birthDate', 'year'],
    });
    return;
  }

  // `docs/07` §5.4: 18 yoshgacha ota-ona roziligi SHART. Kattalarda maydon umuman
  // ko'rsatilmaydi va `false` bo'lib ketaveradi.
  if (age < PARENTAL_CONSENT_AGE && !values.parentalConsent) {
    ctx.addIssue({
      code: 'custom',
      message: '18 yoshgacha ota-ona (qonuniy vakil) roziligi majburiy.',
      path: ['parentalConsent'],
    });
  }
}

const baseSchema = z
  .object({
    fullName: z
      .string()
      .trim()
      .min(
        MIN_FULLNAME_LENGTH,
        `F.I.Sh. kamida ${String(MIN_FULLNAME_LENGTH)} belgidan iborat bo'lishi kerak.`,
      ),
    birthDate: birthDateSchema,
    // Haqiqiy validatsiya `createPublicRegistrationSchema` ning `.extend()`ida
    // `genderRequirement`ga qarab qo'yiladi (`genderRequiredSchema`/`genderLenientSchema`) —
    // bu yerdagi shakl faqat tur (`z.infer`) uchun.
    gender: z.string(),
    /** Bo'sh satr — "maktabda o'qimayman" (serverga `null` ketadi). */
    grade: z.string(),
    phone: z
      .string()
      .regex(/^\d{9}$/, "Telefon raqamini +998 dan keyingi 9 ta raqam bilan to'liq kiriting."),
    email: z
      .string()
      .refine((value) => value === '' || z.string().email().safeParse(value).success, {
        message: "Email formati noto'g'ri.",
      }),
    consentAccepted: z.boolean(),
    parentalConsent: z.boolean(),
  });

/** Birinchi ro'yxatdan o'tish sxemasi (rozilik MAJBURIY) — ilgarigi xatti-harakat aynan saqlangan. */
export const publicRegistrationSchema = createPublicRegistrationSchema({ requireConsent: true });

export type PublicRegistrationFormValues = z.infer<typeof publicRegistrationSchema>;

export const PUBLIC_REGISTRATION_DEFAULT_VALUES: PublicRegistrationFormValues = {
  fullName: '',
  birthDate: { day: '', month: '', year: '' },
  gender: '',
  grade: '',
  phone: '',
  email: '',
  consentAccepted: false,
  parentalConsent: false,
  customFields: {},
};
