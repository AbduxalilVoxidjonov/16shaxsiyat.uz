import { z } from 'zod';

/**
 * Test meta ma'lumotlarini tahrirlash formasi (`PUT /api/admin/catalog/tests/{id}`).
 * Yakuniy haqiqat manbai — backend `UpdateCatalogTestCommandValidator`; bu yerdagi tekshiruv
 * faqat tezroq xabar berish uchun (`programFormSchema.ts` naqshi).
 */
const MAX_NAME_LENGTH = 150;
const MAX_DESCRIPTION_LENGTH = 2000;
const MAX_MINUTES = 240;
const MAX_PAGE_SIZE = 100;
const MAX_DISPLAY_ORDER = 1000;

function integerInRange(min: number, max: number, message: string) {
  return z.number().superRefine((value, ctx) => {
    if (Number.isNaN(value) || !Number.isInteger(value) || value < min || value > max) {
      ctx.addIssue({ code: 'custom', message });
    }
  });
}

export const testMetaSchema = z.object({
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
  displayOrder: integerInRange(
    0,
    MAX_DISPLAY_ORDER,
    `Tartib raqami 0 dan ${String(MAX_DISPLAY_ORDER)} gacha butun son bo'lsin.`,
  ),
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
  shuffleQuestions: z.boolean(),
});

export type TestMetaFormValues = z.infer<typeof testMetaSchema>;
