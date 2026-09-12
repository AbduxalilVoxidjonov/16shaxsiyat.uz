import { z } from 'zod';
import { PROGRAM_VISIBILITY_VALUES, REGISTRATION_MODE_VALUES } from './types';

/**
 * Dastur yaratish/tahrirlash forma validatsiyasi. Backend yakuniy haqiqat manbai
 * (`CreateProgramCommandValidator`/`UpdateProgramCommandValidator`) — bu yerdagi tekshiruv
 * faqat tezroq xabar berish uchun (`schoolFormSchema.ts`dagi izohdagi naqsh).
 */
const MAX_CODE_LENGTH = 50;
const MAX_NAME_LENGTH = 200;
const MAX_DESCRIPTION_LENGTH = 2000;
const MIN_DISPLAY_ORDER = 1;
const MAX_DISPLAY_ORDER = 1000;
const DEFAULT_DISPLAY_ORDER = 1;

const CODE_PATTERN = /^[A-Z0-9_-]+$/;

export const programFormSchema = z.object({
  code: z
    .string()
    .trim()
    .min(1, 'Dastur kodini kiriting.')
    .max(
      MAX_CODE_LENGTH,
      `Kod ko'pi bilan ${String(MAX_CODE_LENGTH)} belgidan iborat bo'lishi kerak.`,
    )
    .refine((value) => CODE_PATTERN.test(value), {
      message:
        "Kod faqat lotin katta harflari, raqam, '_' va '-' belgilaridan iborat bo'lishi kerak.",
    }),
  nameUz: z
    .string()
    .trim()
    .min(1, 'Dastur nomini kiriting.')
    .max(
      MAX_NAME_LENGTH,
      `Nom ko'pi bilan ${String(MAX_NAME_LENGTH)} belgidan iborat bo'lishi kerak.`,
    ),
  descriptionUz: z
    .string()
    .trim()
    .max(
      MAX_DESCRIPTION_LENGTH,
      `Tavsif ko'pi bilan ${String(MAX_DESCRIPTION_LENGTH)} belgidan iborat bo'lishi kerak.`,
    )
    .optional(),
  displayOrder: z.number().superRefine((value, ctx) => {
    if (Number.isNaN(value)) {
      ctx.addIssue({ code: 'custom', message: 'Tartib raqamini kiriting.' });
      return;
    }
    if (!Number.isInteger(value)) {
      ctx.addIssue({ code: 'custom', message: "Tartib raqami butun son bo'lishi kerak." });
      return;
    }
    if (value < MIN_DISPLAY_ORDER || value > MAX_DISPLAY_ORDER) {
      ctx.addIssue({
        code: 'custom',
        message: `Tartib raqami ${String(MIN_DISPLAY_ORDER)} dan ${String(MAX_DISPLAY_ORDER)} gacha bo'lishi kerak.`,
      });
    }
  }),
  visibility: z.enum(PROGRAM_VISIBILITY_VALUES, { message: "Ko'rinishni tanlang." }),
  /**
   * `docs/18` §9 (P52, 2026-09-11). Backend qat'iy invarianti (shaxsiyat batareyasi bo'lsa
   * `None` bo'lolmaydi, `400 REGISTRATION_REQUIRED_FOR_BATTERY`) bu yerda emas —
   * `ProgramFormDialog` tanlovni OLDINDAN bloklaydi (`hasPersonalityBattery`), shu sabab bu
   * sxema faqat qiymat to'g'ri enum ekanini tekshiradi.
   */
  registrationMode: z.enum(REGISTRATION_MODE_VALUES, { message: 'Rejimni tanlang.' }),
});

export type ProgramFormValues = z.infer<typeof programFormSchema>;

export const PROGRAM_FORM_DEFAULT_VALUES: ProgramFormValues = {
  code: '',
  nameUz: '',
  descriptionUz: '',
  displayOrder: DEFAULT_DISPLAY_ORDER,
  visibility: 'Assigned',
  registrationMode: 'Full',
};
