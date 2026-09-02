import { z } from 'zod';
import { QUESTION_TYPE_VALUES } from './types';

/**
 * Savol formasi validatsiyasi. Yakuniy haqiqat manbai — backend
 * `CreateTestQuestionCommandValidator`/`UpdateTestQuestionCommandValidator`.
 *
 * `code` va `type` FAQAT yaratishda talab qilinadi (`PUT questions/{id}` ularni umuman qabul
 * qilmaydi), shu sabab sxema rejim bo'yicha quriladi — lekin ikkala rejimda ham chiqish tipi
 * bir xil, shuning uchun `useForm` ga qo'shimcha kast kerak emas.
 */
const MAX_TEXT_LENGTH = 500;
const MAX_SCALE_LENGTH = 10;
const MAX_CODE_LENGTH = 20;
const MAX_ORDER = 1000;
const MAX_WEIGHT = 10;

const CODE_PATTERN = /^[A-Za-z0-9_-]+$/;

export function createQuestionFormSchema(mode: 'create' | 'edit') {
  return z.object({
    code:
      mode === 'create'
        ? z
            .string()
            .trim()
            .min(1, 'Savol kodini kiriting.')
            .max(MAX_CODE_LENGTH, `Kod ko'pi bilan ${String(MAX_CODE_LENGTH)} belgi bo'lsin.`)
            .refine((value) => CODE_PATTERN.test(value), {
              message: "Kod faqat lotin harflari, raqam, '-' va '_' belgilaridan iborat bo'lsin.",
            })
        : z.string(),
    type: z.enum(QUESTION_TYPE_VALUES, { message: 'Savol turini tanlang.' }),
    textUz: z
      .string()
      .trim()
      .min(1, 'Savol matnini kiriting.')
      .max(MAX_TEXT_LENGTH, `Matn ko'pi bilan ${String(MAX_TEXT_LENGTH)} belgi bo'lsin.`),
    textRu: z
      .string()
      .trim()
      .max(MAX_TEXT_LENGTH, `Matn ko'pi bilan ${String(MAX_TEXT_LENGTH)} belgi bo'lsin.`),
    textEn: z
      .string()
      .trim()
      .max(MAX_TEXT_LENGTH, `Matn ko'pi bilan ${String(MAX_TEXT_LENGTH)} belgi bo'lsin.`),
    order: z.number().superRefine((value, ctx) => {
      if (Number.isNaN(value) || !Number.isInteger(value) || value < 1 || value > MAX_ORDER) {
        ctx.addIssue({
          code: 'custom',
          message: `Tartib raqami 1 dan ${String(MAX_ORDER)} gacha butun son bo'lsin.`,
        });
      }
    }),
    isActive: z.boolean(),
    isRequired: z.boolean(),
    scale: z
      .string()
      .trim()
      .min(1, 'Shkala kodini kiriting.')
      .max(MAX_SCALE_LENGTH, `Shkala kodi ko'pi bilan ${String(MAX_SCALE_LENGTH)} belgi bo'lsin.`),
    direction: z.union([z.literal(1), z.literal(-1)], {
      message: "Yo'nalish faqat +1 yoki -1 bo'lishi mumkin.",
    }),
    weight: z.number().superRefine((value, ctx) => {
      if (Number.isNaN(value) || value <= 0 || value > MAX_WEIGHT) {
        ctx.addIssue({
          code: 'custom',
          message: `Og'irlik 0 dan katta va ${String(MAX_WEIGHT)} dan kichik bo'lsin.`,
        });
      }
    }),
  });
}

export type QuestionDialogFormValues = z.infer<ReturnType<typeof createQuestionFormSchema>>;
