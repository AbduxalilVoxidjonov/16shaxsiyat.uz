import { z } from 'zod';

/**
 * Shkala formasi (`POST /tests/{id}/scales`, `PUT /scales/{scaleId}`) — faqat `Custom`
 * testlarda. Backend `CreateTestScaleCommandValidator`/`UpdateTestScaleCommandValidator`
 * yakuniy haqiqat manbai.
 */
const MAX_CODE_LENGTH = 10;
const MAX_NAME_LENGTH = 120;
const MAX_DESCRIPTION_LENGTH = 500;
const MAX_DISPLAY_ORDER = 1000;

const CODE_PATTERN = /^[A-Z0-9_-]+$/;

export function createScaleFormSchema(mode: 'create' | 'edit') {
  return z.object({
    code:
      mode === 'create'
        ? z
            .string()
            .trim()
            .min(1, 'Shkala kodini kiriting.')
            .max(MAX_CODE_LENGTH, `Kod ko'pi bilan ${String(MAX_CODE_LENGTH)} belgi bo'lsin.`)
            .refine((value) => CODE_PATTERN.test(value), {
              message:
                "Kod faqat katta lotin harflari, raqam, '-' va '_' belgilaridan iborat bo'lsin.",
            })
        : z.string(),
    nameUz: z
      .string()
      .trim()
      .min(1, 'Shkala nomini kiriting.')
      .max(MAX_NAME_LENGTH, `Nom ko'pi bilan ${String(MAX_NAME_LENGTH)} belgi bo'lsin.`),
    descriptionUz: z
      .string()
      .trim()
      .max(
        MAX_DESCRIPTION_LENGTH,
        `Tavsif ko'pi bilan ${String(MAX_DESCRIPTION_LENGTH)} belgi bo'lsin.`,
      ),
    displayOrder: z.number().superRefine((value, ctx) => {
      if (
        Number.isNaN(value) ||
        !Number.isInteger(value) ||
        value < 0 ||
        value > MAX_DISPLAY_ORDER
      ) {
        ctx.addIssue({
          code: 'custom',
          message: `Tartib raqami 0 dan ${String(MAX_DISPLAY_ORDER)} gacha butun son bo'lsin.`,
        });
      }
    }),
  });
}

export type ScaleFormValues = z.infer<ReturnType<typeof createScaleFormSchema>>;
