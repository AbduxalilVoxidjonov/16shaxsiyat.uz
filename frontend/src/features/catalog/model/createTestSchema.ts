import { z } from 'zod';
import { TEST_SCORING_MODE_VALUES } from './types';

/**
 * "Yangi anketa" formasi (`POST /api/admin/catalog/tests`). Yakuniy haqiqat manbai — backend
 * `CreateCatalogTestCommandValidator`; bu yerdagi tekshiruv faqat tezroq xabar berish uchun
 * (`testMetaSchema.ts` naqshi) va qoidalar backend bilan AYNAN bir xil bo'lishi shart:
 * kod `^[A-Z0-9_-]+$`, ko'pi bilan 20 belgi.
 */
const MAX_CODE_LENGTH = 20;
const MAX_NAME_LENGTH = 150;
const MAX_DESCRIPTION_LENGTH = 2000;
const MAX_MINUTES = 240;
const MAX_PAGE_SIZE = 100;

function integerInRange(min: number, max: number, message: string) {
  return z.number().superRefine((value, ctx) => {
    if (Number.isNaN(value) || !Number.isInteger(value) || value < min || value > max) {
      ctx.addIssue({ code: 'custom', message });
    }
  });
}

export const createTestSchema = z.object({
  code: z
    .string()
    .trim()
    .min(1, 'Anketa kodini kiriting.')
    .max(MAX_CODE_LENGTH, `Kod ko'pi bilan ${String(MAX_CODE_LENGTH)} belgidan iborat bo'lsin.`)
    .regex(
      /^[A-Z0-9_-]+$/,
      "Kod faqat lotin KATTA harflari, raqam, '_' va '-' belgilaridan iborat bo'lishi kerak.",
    ),
  nameUz: z
    .string()
    .trim()
    .min(1, 'Anketa nomini kiriting.')
    .max(MAX_NAME_LENGTH, `Nom ko'pi bilan ${String(MAX_NAME_LENGTH)} belgidan iborat bo'lsin.`),
  descriptionUz: z
    .string()
    .trim()
    .max(
      MAX_DESCRIPTION_LENGTH,
      `Tavsif ko'pi bilan ${String(MAX_DESCRIPTION_LENGTH)} belgidan iborat bo'lsin.`,
    )
    .optional(),
  estimatedMinutes: integerInRange(
    1,
    MAX_MINUTES,
    `Taxminiy vaqt 1 dan ${String(MAX_MINUTES)} daqiqagacha butun son bo'lsin.`,
  ),
  pageSize: integerInRange(
    1,
    MAX_PAGE_SIZE,
    `Sahifadagi savollar soni 1 dan ${String(MAX_PAGE_SIZE)} gacha butun son bo'lsin.`,
  ),
  scoringMode: z.enum(TEST_SCORING_MODE_VALUES),
});

export type CreateTestFormValues = z.infer<typeof createTestSchema>;
