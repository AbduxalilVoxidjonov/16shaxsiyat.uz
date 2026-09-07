import { z } from 'zod';
import { UZBEKISTAN_REGIONS } from './regions';

/**
 * Yaratish/tahrirlash drawer validatsiyasi — `docs/02-biznes-talablar.md` FR-1.1
 * ("Maktab yozuvi: nomi, viloyat, tuman, maktab raqami, mas'ul FISH, telefon, izoh"),
 * `docs/05-database-schema.md` `schools` jadval ustun uzunliklari (`name varchar(200)`,
 * `region`/`district varchar(100)`, `school_number varchar(20)`, `contact_person varchar(150)`,
 * `contact_phone varchar(20)`, `notes varchar(1000)`). `access_code` — admin UI'dan olib
 * tashlangan (2026-09-07), backend'da ixtiyoriy/eskirgan; o'rnini avtomatik `entryCode` egalladi.
 *
 * Backend yakuniy haqiqat manbai — bu yerdagi tekshiruv faqat tezroq xabar berish uchun
 * (xuddi `features/settings/model/changePasswordSchema.ts`dagi izohdagi naqsh).
 */
const MAX_NAME_LENGTH = 200;
const MAX_REGION_DISTRICT_LENGTH = 100;
const MAX_SCHOOL_NUMBER_LENGTH = 20;
const MAX_CONTACT_PERSON_LENGTH = 150;
const MAX_CONTACT_PHONE_LENGTH = 20;
const MAX_NOTES_LENGTH = 1000;
const MIN_DAILY_LIMIT = 1;
const MAX_DAILY_LIMIT = 100000;
const DEFAULT_DAILY_LIMIT = 500;

/**
 * Ixtiyoriy matn maydoni — uzunlik tekshiruvi bilan. **Ataylab `.transform()` ishlatilmagan**:
 * zod'da `.optional().transform(fn)` kirish (input) va chiqish (output) tiplarini turlicha
 * qiladi (`schoolNumber?: string` kirishda, lekin `schoolNumber: string | undefined` chiqishda),
 * bu esa `zodResolver`ning `useForm<SchoolFormValues>` bilan ziddiyatiga olib keladi (TS2322).
 * Shu sabab bo'sh matnni `undefined`ga o'girish submit vaqtida chaqiruvchi tomonda qilinadi
 * (`SchoolFormDialog.tsx`, `emptyToUndefined`).
 */
function optionalTrimmed(maxLength: number, message: string) {
  return z.string().trim().max(maxLength, message).optional();
}

export const schoolFormSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "Maktab nomini kiriting.")
    .max(MAX_NAME_LENGTH, `Nom ko'pi bilan ${String(MAX_NAME_LENGTH)} belgidan iborat bo'lishi kerak.`),
  region: z
    .string()
    .refine((value) => UZBEKISTAN_REGIONS.includes(value), { message: 'Viloyatni tanlang.' }),
  district: z
    .string()
    .trim()
    .min(1, 'Tumanni kiriting.')
    .max(
      MAX_REGION_DISTRICT_LENGTH,
      `Tuman nomi ko'pi bilan ${String(MAX_REGION_DISTRICT_LENGTH)} belgidan iborat bo'lishi kerak.`,
    ),
  schoolNumber: optionalTrimmed(
    MAX_SCHOOL_NUMBER_LENGTH,
    `Maktab raqami ko'pi bilan ${String(MAX_SCHOOL_NUMBER_LENGTH)} belgidan iborat bo'lishi kerak.`,
  ),
  contactPerson: optionalTrimmed(
    MAX_CONTACT_PERSON_LENGTH,
    `Mas'ul shaxs ismi ko'pi bilan ${String(MAX_CONTACT_PERSON_LENGTH)} belgidan iborat bo'lishi kerak.`,
  ),
  contactPhone: optionalTrimmed(
    MAX_CONTACT_PHONE_LENGTH,
    `Telefon raqami ko'pi bilan ${String(MAX_CONTACT_PHONE_LENGTH)} belgidan iborat bo'lishi kerak.`,
  ),
  // `Input`dan `valueAsNumber: true` bilan keladi — bo'sh bo'lsa `NaN` (`registrationSchema.ts`
  // dagi `birthDateSchema.superRefine` naqshiga o'xshash qo'lda tekshiruv).
  dailyRegistrationLimit: z.number().superRefine((value, ctx) => {
    if (Number.isNaN(value)) {
      ctx.addIssue({ code: 'custom', message: 'Kunlik limitni kiriting.' });
      return;
    }
    if (!Number.isInteger(value)) {
      ctx.addIssue({ code: 'custom', message: "Kunlik limit butun son bo'lishi kerak." });
      return;
    }
    if (value < MIN_DAILY_LIMIT || value > MAX_DAILY_LIMIT) {
      ctx.addIssue({
        code: 'custom',
        message: `Kunlik limit ${String(MIN_DAILY_LIMIT)} dan ${String(MAX_DAILY_LIMIT)} gacha bo'lishi kerak.`,
      });
    }
  }),
  notes: optionalTrimmed(
    MAX_NOTES_LENGTH,
    `Izoh ko'pi bilan ${String(MAX_NOTES_LENGTH)} belgidan iborat bo'lishi kerak.`,
  ),
});

export type SchoolFormValues = z.infer<typeof schoolFormSchema>;

export const SCHOOL_FORM_DEFAULT_VALUES: SchoolFormValues = {
  name: '',
  region: '',
  district: '',
  schoolNumber: '',
  contactPerson: '',
  contactPhone: '',
  dailyRegistrationLimit: DEFAULT_DAILY_LIMIT,
  notes: '',
};
