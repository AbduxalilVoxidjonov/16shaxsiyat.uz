import { z } from 'zod';
import { calculateAge, isValidCalendarDate } from '@/shared/lib/birthDate';

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

/**
 * Sxema PROFIL HOLATIGA bog'liq (`lib/profileState.ts`): rozilik joriy bo'lsa (`ready`/`edit`
 * profil bilan) blok ko'rsatilmaydi va `consentAccepted` talab qilinmaydi — server ham shu
 * holatda talab qilmaydi (`docs/07` §5.4). Qolgan qoidalar (F.I.Sh., sana, jins, telefon,
 * 18 yoshgacha ota-ona roziligi) uchala holatda bir xil — mavjud profil tahririda ham forma
 * to'liq yuboriladi.
 */
export interface PublicRegistrationSchemaOptions {
  /** `false` — rozilik joriy, checkbox ko'rsatilmaydi va tekshirilmaydi. */
  requireConsent: boolean;
}

export function createPublicRegistrationSchema({ requireConsent }: PublicRegistrationSchemaOptions) {
  return baseSchema
    .extend({
      consentAccepted: requireConsent
        ? z
            .boolean()
            .refine((value) => value === true, { message: "Roziliksiz testni boshlab bo'lmaydi." })
        : z.boolean(),
    })
    .superRefine(refineBirthDateAndParentalConsent);
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
    // `includes()` ataylab: to'g'ridan-to'g'ri tenglik tekshiruvi TS 5.5+ da chiqish tipini
    // toraytirib, RHF `defaultValues` dagi bo'sh `''` bilan mos kelmay qolardi.
    gender: z
      .string()
      .refine((value) => (['Male', 'Female'] as const).includes(value as 'Male' | 'Female'), {
        message: 'Jinsni tanlang.',
      }),
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
};
