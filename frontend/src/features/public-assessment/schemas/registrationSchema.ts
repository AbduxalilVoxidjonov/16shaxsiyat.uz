import { z } from 'zod';
import { birthDateToIso, calculateAge, isValidCalendarDate } from '@/shared/lib/birthDate';
import {
  DEFAULT_REGISTRATION_FIELDS,
  type RegistrationFields,
} from '@/shared/api/registrationModeTypes';

/**
 * E-2 anketa validatsiyasi — `docs/11` E-2, `prompts/20`. Yosh oralig'i va F.I.Sh. uzunligi
 * backend `StartSessionCommandValidator`ga aynan mos (`src/StudentRoadMap.Application/Public/
 * StartSession/StartSessionCommandValidator.cs`) — ikkalasi ham 6–20 yosh va 5 belgidan
 * kam bo'lmagan F.I.Sh. talab qiladi; frontend faqat tezroq, do'stona xabar berish uchun
 * oldindan tekshiradi, yakuniy haqiqat manbai baribir backend.
 */
export const MIN_AGE = 6;
export const MAX_AGE = 20;
const MIN_FULLNAME_LENGTH = 5;
const ACCESS_CODE_LENGTH = 6;

/**
 * Sana hisobi endi `shared/lib/birthDate` da — ommaviy (maktabsiz) anketa bilan UMUMIY
 * (yosh chegarasi farq qiladi, algoritm emas). Eski nomlar shu yerdan re-export qilinadi,
 * chunki ular bu modulning ommaviy API'si bo'lib qolgan.
 */
export { calculateAge, birthDateToIso };

/** `birthDateRequiredSchema`/`birthDateLenientSchema`ning umumiy sana tekshiruvi (BARCHA uchta bo'lak to'ldirilganda). */
function validateFilledBirthDate(
  day: number,
  month: number,
  year: number,
  ctx: z.RefinementCtx,
): void {
  if (!isValidCalendarDate(day, month, year)) {
    ctx.addIssue({ code: 'custom', message: 'Bunday sana mavjud emas.', path: ['day'] });
    return;
  }

  const age = calculateAge(new Date(Date.UTC(year, month - 1, day)), new Date());
  if (age < MIN_AGE || age > MAX_AGE) {
    ctx.addIssue({
      code: 'custom',
      message: `Tug'ilgan sana ${String(MIN_AGE)}-${String(MAX_AGE)} yosh oralig'iga to'g'ri kelishi kerak.`,
      path: ['year'],
    });
  }
}

/** `registrationFields.birthDate === 'Required'` (standart) — hozirgi (o'zgarmagan) xatti-harakat. */
const birthDateRequiredSchema = z
  .object({
    day: z.string().min(1, 'Kunni tanlang.'),
    month: z.string().min(1, "Oyni tanlang."),
    year: z.string().min(1, 'Yilni tanlang.'),
  })
  .superRefine((value, ctx) => {
    if (!value.day || !value.month || !value.year) {
      // Bo'sh maydonlar allaqachon yuqoridagi `min(1)` orqali xabar beradi.
      return;
    }
    validateFilledBirthDate(Number(value.day), Number(value.month), Number(value.year), ctx);
  });

/**
 * `registrationFields.birthDate === 'Optional' | 'Hidden'` — bo'sh qoldirish mumkin, lekin
 * qisman to'ldirilsa (masalan faqat kun/oy tanlangan) xato beradi: yarim sana saqlanmasin.
 */
const birthDateLenientSchema = z
  .object({
    day: z.string(),
    month: z.string(),
    year: z.string(),
  })
  .superRefine((value, ctx) => {
    const filledCount = [value.day, value.month, value.year].filter((part) => part !== '').length;
    if (filledCount === 0) {
      return;
    }
    if (filledCount < 3) {
      ctx.addIssue({
        code: 'custom',
        message: "Tug'ilgan sanani to'liq kiriting yoki bo'sh qoldiring.",
        path: ['day'],
      });
      return;
    }
    validateFilledBirthDate(Number(value.day), Number(value.month), Number(value.year), ctx);
  });

// Eslatma: TS 5.5+ "inferred type predicates" tufayli `value === 'Male' || value === 'Female'`
// kabi to'g'ridan-to'g'ri tenglik tekshiruvi zod chiqish tipini `'Male' | 'Female'`ga
// toraytirib qo'yadi (RHF `defaultValues`dagi bo'sh `''` bilan mos kelmay qoladi) —
// shu sabab `includes()` ataylab ishlatilgan (TS bu shaklni predikat sifatida chiqarmaydi).
const genderRequiredSchema = z
  .string()
  .refine((value) => (['Male', 'Female'] as const).includes(value as 'Male' | 'Female'), {
    message: 'Jinsni tanlang.',
  });
const genderLenientSchema = z
  .string()
  .refine(
    (value) => value === '' || (['Male', 'Female'] as const).includes(value as 'Male' | 'Female'),
    { message: 'Jinsni tanlang.' },
  );

const gradeRequiredSchema = z.string().min(1, 'Sinfni tanlang.');
const gradeLenientSchema = z.string();

const CLASS_LETTER_MAX_MESSAGE = "Sinf harfi 2 belgidan oshmasligi kerak.";
const classLetterRequiredSchema = z
  .string()
  .trim()
  .min(1, 'Sinf harfini kiriting.')
  .max(2, CLASS_LETTER_MAX_MESSAGE);
const classLetterLenientSchema = z.string().max(2, CLASS_LETTER_MAX_MESSAGE);

// Eslatma (nomlash): maydon nomi backend `StartSessionCommand.Phone`/`ParentPhone`
// (camelCase xato javobida `phone`/`parentPhone`) bilan ATAYLAB bir xil — shu sabab
// 400 validatsiya xatolarini `RegistrationPage` alohida xarita (map) siz, to'g'ridan-to'g'ri
// `setError(key, ...)` bilan bog'lay oladi. Qiymatning o'zi baribir faqat mahalliy 9 ta
// raqam (`+998`siz) — E.164'ga o'girish yuborishdan oldin (`toE164UzPhone`).
const phoneRequiredSchema = z
  .string()
  .regex(/^\d{9}$/, "Telefon raqamini +998 dan keyingi 9 ta raqam bilan to'liq kiriting.");
const phoneLenientSchema = z.string().refine((value) => value === '' || /^\d{9}$/.test(value), {
  message: "Telefon raqamini to'liq kiriting yoki bo'sh qoldiring.",
});

const parentPhoneRequiredSchema = z
  .string()
  .regex(
    /^\d{9}$/,
    "Ota-ona telefon raqamini +998 dan keyingi 9 ta raqam bilan to'liq kiriting.",
  );
const parentPhoneLenientSchema = z
  .string()
  .refine((value) => value === '' || /^\d{9}$/.test(value), {
    message: "Ota-ona telefon raqamini to'liq kiriting yoki bo'sh qoldiring.",
  });

const EMAIL_INVALID_MESSAGE = "Email formati noto'g'ri.";
const emailRequiredSchema = z
  .string()
  .refine((value) => value !== '' && z.string().email().safeParse(value).success, {
    message: EMAIL_INVALID_MESSAGE,
  });
const emailLenientSchema = z
  .string()
  .refine((value) => value === '' || z.string().email().safeParse(value).success, {
    message: EMAIL_INVALID_MESSAGE,
  });

/**
 * `requiresAccessCode` maktab ma'lumotidan (`GET /schools/{slug}`) keladi — shu sabab sxema
 * dinamik quriladi (`RegistrationPage` `useMemo` bilan chaqiradi). `registrationFields` —
 * dasturning har bir shaxs maydoni uchun "Yashirin"/"Ixtiyoriy"/"Majburiy" sozlamasi (P52,
 * `docs/18` §9); berilmasa `DEFAULT_REGISTRATION_FIELDS` (hozirgi, P52dan oldingi xatti-harakat
 * — regressiya qulfi) ishlatiladi. `Hidden` maydon uchun ham "lenient" (bo'sh qoldirish mumkin)
 * sxema ishlatiladi — maydon UI'da ko'rsatilmagani sabab qiymati baribir bo'sh qoladi.
 */
export function buildRegistrationSchema(
  requiresAccessCode: boolean,
  registrationFields: RegistrationFields = DEFAULT_REGISTRATION_FIELDS,
) {
  const isRequired = (key: keyof RegistrationFields) => registrationFields[key] === 'Required';

  return z.object({
    fullName: z
      .string()
      .trim()
      .min(
        MIN_FULLNAME_LENGTH,
        `F.I.Sh. kamida ${String(MIN_FULLNAME_LENGTH)} belgidan iborat bo'lishi kerak.`,
      ),
    birthDate: isRequired('birthDate') ? birthDateRequiredSchema : birthDateLenientSchema,
    gender: isRequired('gender') ? genderRequiredSchema : genderLenientSchema,
    grade: isRequired('grade') ? gradeRequiredSchema : gradeLenientSchema,
    classLetter: isRequired('classLetter') ? classLetterRequiredSchema : classLetterLenientSchema,
    phone: isRequired('phone') ? phoneRequiredSchema : phoneLenientSchema,
    parentPhone: isRequired('parentPhone') ? parentPhoneRequiredSchema : parentPhoneLenientSchema,
    email: isRequired('email') ? emailRequiredSchema : emailLenientSchema,
    consentAccepted: z
      .boolean()
      .refine((value) => value === true, { message: "Roziliksiz ro'yxatdan o'tib bo'lmaydi." }),
    accessCode: requiresAccessCode
      ? z.string().regex(new RegExp(`^\\d{${String(ACCESS_CODE_LENGTH)}}$`), '6 xonali kirish kodini kiriting.')
      : z.string(),
  });
}

export type RegistrationFormValues = z.infer<ReturnType<typeof buildRegistrationSchema>>;

export const REGISTRATION_DEFAULT_VALUES: RegistrationFormValues = {
  fullName: '',
  birthDate: { day: '', month: '', year: '' },
  gender: '',
  grade: '',
  classLetter: '',
  phone: '',
  parentPhone: '',
  email: '',
  consentAccepted: false,
  accessCode: '',
};
