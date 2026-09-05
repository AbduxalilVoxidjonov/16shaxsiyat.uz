import { z } from 'zod';
import { birthDateToIso, calculateAge, isValidCalendarDate } from '@/shared/lib/birthDate';

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

const birthDateSchema = z
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

    const day = Number(value.day);
    const month = Number(value.month);
    const year = Number(value.year);

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
  });

/**
 * `requiresAccessCode` maktab ma'lumotidan (`GET /schools/{slug}`) keladi — shu sabab sxema
 * dinamik quriladi (`RegistrationPage` `useMemo` bilan chaqiradi).
 */
export function buildRegistrationSchema(requiresAccessCode: boolean) {
  return z.object({
    fullName: z
      .string()
      .trim()
      .min(
        MIN_FULLNAME_LENGTH,
        `F.I.Sh. kamida ${String(MIN_FULLNAME_LENGTH)} belgidan iborat bo'lishi kerak.`,
      ),
    birthDate: birthDateSchema,
    // Eslatma: TS 5.5+ "inferred type predicates" tufayli `value === 'Male' || value === 'Female'`
    // kabi to'g'ridan-to'g'ri tenglik tekshiruvi zod chiqish tipini `'Male' | 'Female'`ga
    // toraytirib qo'yadi (RHF `defaultValues`dagi bo'sh `''` bilan mos kelmay qoladi) —
    // shu sabab `includes()` ataylab ishlatilgan (TS bu shaklni predikat sifatida chiqarmaydi).
    gender: z
      .string()
      .refine((value) => (['Male', 'Female'] as const).includes(value as 'Male' | 'Female'), {
        message: 'Jinsni tanlang.',
      }),
    grade: z.string().min(1, 'Sinfni tanlang.'),
    classLetter: z.string().max(2, "Sinf harfi 2 belgidan oshmasligi kerak."),
    // Eslatma (nomlash): maydon nomi backend `StartSessionCommand.Phone`/`ParentPhone`
    // (camelCase xato javobida `phone`/`parentPhone`) bilan ATAYLAB bir xil — shu sabab
    // 400 validatsiya xatolarini `RegistrationPage` alohida xarita (map) siz, to'g'ridan-to'g'ri
    // `setError(key, ...)` bilan bog'lay oladi. Qiymatning o'zi baribir faqat mahalliy 9 ta
    // raqam (`+998`siz) — E.164'ga o'girish yuborishdan oldin (`toE164UzPhone`).
    phone: z
      .string()
      .regex(/^\d{9}$/, "Telefon raqamini +998 dan keyingi 9 ta raqam bilan to'liq kiriting."),
    parentPhone: z
      .string()
      .refine((value) => value === '' || /^\d{9}$/.test(value), {
        message: "Ota-ona telefon raqamini to'liq kiriting yoki bo'sh qoldiring.",
      }),
    email: z.string().refine((value) => value === '' || z.string().email().safeParse(value).success, {
      message: "Email formati noto'g'ri.",
    }),
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
