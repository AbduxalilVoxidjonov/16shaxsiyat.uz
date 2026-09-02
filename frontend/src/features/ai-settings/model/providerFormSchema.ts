import { z } from 'zod';

/**
 * Provayder kartasi formasi — `docs/07` 3.5-bo'lim, `docs/04` 2.9-bo'lim (`AiProviderConfig`
 * maydonlari). Backend yakuniy haqiqat manbai — bu yerdagi tekshiruv faqat tezroq xabar
 * berish uchun (`schools/model/schoolFormSchema.ts`dagi izohdagi naqsh).
 */
const MAX_MODEL_LENGTH = 100;
const MIN_MAX_OUTPUT_TOKENS = 1;
const MAX_MAX_OUTPUT_TOKENS = 32_768;
const MIN_TEMPERATURE = 0;
const MAX_TEMPERATURE = 2;

export const providerFormSchema = z.object({
  model: z
    .string()
    .trim()
    .min(1, 'Model nomini kiriting.')
    .max(MAX_MODEL_LENGTH, `Model nomi ko'pi bilan ${String(MAX_MODEL_LENGTH)} belgidan iborat bo'lishi kerak.`),
  // `Input`dan `valueAsNumber: true` bilan keladi — bo'sh bo'lsa `NaN`.
  maxOutputTokens: z.number().superRefine((value, ctx) => {
    if (Number.isNaN(value)) {
      ctx.addIssue({ code: 'custom', message: 'Maksimal token sonini kiriting.' });
      return;
    }
    if (!Number.isInteger(value) || value < MIN_MAX_OUTPUT_TOKENS || value > MAX_MAX_OUTPUT_TOKENS) {
      ctx.addIssue({
        code: 'custom',
        message: `Token soni ${String(MIN_MAX_OUTPUT_TOKENS)} dan ${String(MAX_MAX_OUTPUT_TOKENS)} gacha butun son bo'lishi kerak.`,
      });
    }
  }),
  temperature: z.number().superRefine((value, ctx) => {
    if (Number.isNaN(value)) {
      ctx.addIssue({ code: 'custom', message: "Temperature qiymatini kiriting." });
      return;
    }
    if (value < MIN_TEMPERATURE || value > MAX_TEMPERATURE) {
      ctx.addIssue({
        code: 'custom',
        message: `Temperature ${String(MIN_TEMPERATURE)} dan ${String(MAX_TEMPERATURE)} gacha bo'lishi kerak.`,
      });
    }
  }),
  isActive: z.boolean(),
  /** Bo'sh — kalit o'zgarmaydi (`ApiKeyField.tsx`). Format provider bo'yicha farqlanadi, shu sabab qat'iy regex yo'q. */
  apiKey: z.string(),
});

export type ProviderFormValues = z.infer<typeof providerFormSchema>;
