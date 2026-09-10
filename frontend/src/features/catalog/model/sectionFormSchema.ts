import { z } from 'zod';

/**
 * Bo'lim formasi (`POST /tests/{id}/sections`, `PUT /sections/{sectionId}`) — faqat `Custom`
 * testlarda (`docs/18` §2.2, §5). Yakuniy haqiqat manbai — backend validatori.
 */
const MAX_CODE_LENGTH = 20;
const MAX_TITLE_LENGTH = 200;
const MAX_DESCRIPTION_LENGTH = 1000;
const MAX_DISPLAY_ORDER = 1000;

const CODE_PATTERN = /^[A-Za-z0-9_-]+$/;

export function createSectionFormSchema(mode: 'create' | 'edit') {
  return z.object({
    code:
      mode === 'create'
        ? z
            .string()
            .trim()
            .min(1, "Bo'lim kodini kiriting.")
            .max(MAX_CODE_LENGTH, `Kod ko'pi bilan ${String(MAX_CODE_LENGTH)} belgi bo'lsin.`)
            .refine((value) => CODE_PATTERN.test(value), {
              message: "Kod faqat lotin harflari, raqam, '-' va '_' belgilaridan iborat bo'lsin.",
            })
        : z.string(),
    titleUz: z
      .string()
      .trim()
      .min(1, 'Sarlavhani kiriting.')
      .max(MAX_TITLE_LENGTH, `Sarlavha ko'pi bilan ${String(MAX_TITLE_LENGTH)} belgi bo'lsin.`),
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

export type SectionFormValues = z.infer<ReturnType<typeof createSectionFormSchema>>;
