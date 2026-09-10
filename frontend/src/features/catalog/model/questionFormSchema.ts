import { z } from 'zod';
import type { VisibilityRule } from '@/shared/lib/visibility';
import {
  isChoiceQuestionType,
  isSurveyOnlyQuestionType,
  isTextQuestionType,
  QUESTION_TYPE_VALUES,
} from './types';

/**
 * Savol formasi validatsiyasi. Yakuniy haqiqat manbai — backend
 * `CreateTestQuestionCommandValidator`/`UpdateTestQuestionCommandValidator`.
 *
 * `code` va `type` FAQAT yaratishda talab qilinadi (`PUT questions/{id}` ularni umuman qabul
 * qilmaydi), shu sabab sxema rejim bo'yicha quriladi — lekin ikkala rejimda ham chiqish tipi
 * bir xil, shuning uchun `useForm` ga qo'shimcha kast kerak emas.
 *
 * `docs/18` §2.1–§2.3 kengaytmasi: `sectionCode`/`visibility`/`placeholder`/`inputPattern`/
 * `maxLength`/`minSelections`/`maxSelections`/`options` — turga qarab QISMAN talab qilinadi
 * (butun obyekt darajasidagi `superRefine`, quyida). Ko'pchiligi IXTIYORIY: bo'sh qoldirilsa
 * backend standart qiymat qo'yadi (`docs/18` §2.3 jadvali) — forma faqat KIRITILGAN qiymat
 * chegarasini tekshiradi, bo'shni majburlamaydi.
 */
const MAX_TEXT_LENGTH = 500;
const MAX_SCALE_LENGTH = 10;
const MAX_CODE_LENGTH = 20;
const MAX_ORDER = 1000;
const MAX_WEIGHT = 10;
const MAX_PLACEHOLDER_LENGTH = 200;
const MAX_INPUT_PATTERN_LENGTH = 200;
const MAX_MAX_LENGTH = 4000;
const MAX_SECTION_CODE_LENGTH = 20;
const MAX_OPTION_TEXT_LENGTH = 200;
const MAX_CONDITIONS = 10;

const CODE_PATTERN = /^[A-Za-z0-9_-]+$/;

/** `inputPattern` .NET va JS ikkalasida ham ishlaydigan regex bo'lishi shart (`docs/18` §2.3). */
function isCompilableRegex(pattern: string): boolean {
  try {
    new RegExp(pattern);
    return true;
  } catch {
    return false;
  }
}

const optionFormSchema = z.object({
  textUz: z
    .string()
    .trim()
    .min(1, 'Variant matnini kiriting.')
    .max(MAX_OPTION_TEXT_LENGTH, `Variant matni ko'pi bilan ${String(MAX_OPTION_TEXT_LENGTH)} belgi bo'lsin.`),
  value: z.number(),
  displayOrder: z.number(),
});

/**
 * `visibility` — `VisibilityRuleEditor` allaqachon FAQAT to'g'ri shakl (bo'sh bo'lmagan
 * kod, ma'lum operator, mos qiymatlar) yaratadi, shu sabab bu yerda struktura emas, faqat
 * shartlar SONI tekshiriladi (`docs/18` §2.4: 1..10). `z.custom` ataylab: zod obyekt
 * sxemasi (`z.object({conditions: z.array(...)})`) chiqargan tur `conditions`ni MUTABLE
 * (`T[]`) deb belgilaydi, `VisibilityRule.conditions` esa `readonly VisibilityCondition[]`
 * (`shared/lib/visibility.ts`) — ikkalasi mos kelmay, `setValue`/`reset` chaqiruvlarida
 * `readonly` massivni mutable joyga berib bo'lmasligi haqida xato berardi.
 */
function isVisibilityRuleLike(value: unknown): value is VisibilityRule {
  return typeof value === 'object' && value !== null && 'match' in value && 'conditions' in value;
}

export function createQuestionFormSchema(mode: 'create' | 'edit') {
  return z
    .object({
      code:
        mode === 'create'
          ? z
              .string()
              .trim()
              .min(1, 'Savol kodini kiriting.')
              .max(MAX_CODE_LENGTH, `Kod ko'pi bilan ${String(MAX_CODE_LENGTH)} belgi bo'lsin.`)
              .refine((value) => CODE_PATTERN.test(value), {
                message:
                  "Kod faqat lotin harflari, raqam, '-' va '_' belgilaridan iborat bo'lsin.",
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
        .max(MAX_SCALE_LENGTH, `Shkala kodi ko'pi bilan ${String(MAX_SCALE_LENGTH)} belgi bo'lsin.`),
      direction: z.union([z.literal(1), z.literal(-1)], {
        message: "Yo'nalish faqat +1 yoki -1 bo'lishi mumkin.",
      }),
      weight: z.number(),
      // `docs/18` §2.2–§2.3 kengaytmasi — bari IXTIYORIY, turga qarab pastda tekshiriladi.
      sectionCode: z
        .string()
        .trim()
        .max(MAX_SECTION_CODE_LENGTH, `Kod ko'pi bilan ${String(MAX_SECTION_CODE_LENGTH)} belgi bo'lsin.`),
      placeholder: z
        .string()
        .max(MAX_PLACEHOLDER_LENGTH, `Ko'pi bilan ${String(MAX_PLACEHOLDER_LENGTH)} belgi bo'lsin.`),
      inputPattern: z
        .string()
        .max(MAX_INPUT_PATTERN_LENGTH, `Ko'pi bilan ${String(MAX_INPUT_PATTERN_LENGTH)} belgi bo'lsin.`)
        .refine((value) => value.trim() === '' || isCompilableRegex(value), {
          message: "Regex noto'g'ri — sinov bekor bo'ldi (masalan qavslar yopilmagan).",
        }),
      // `z.number()` NaN'ni ATAYLAB rad etadi (zod invarianti) — bu uch maydon ixtiyoriy
      // (bo'sh qoldirilsa backend standart qiymat qo'yadi, `docs/18` §2.3), bo'sh raqam
      // input'i esa RHF `valueAsNumber`da har doim `NaN` beradi, shu sabab `z.nan()` ham
      // qabul qilinadi.
      maxLength: z.union([z.number(), z.nan()]),
      minSelections: z.union([z.number(), z.nan()]),
      maxSelections: z.union([z.number(), z.nan()]),
      options: z.array(optionFormSchema),
      visibility: z.custom<VisibilityRule | null>(
        (value) => value === null || isVisibilityRuleLike(value),
      ),
    })
    .superRefine((values, ctx) => {
      const type = values.type;

      if (!isSurveyOnlyQuestionType(type)) {
        if (values.scale.trim().length === 0) {
          ctx.addIssue({ code: 'custom', path: ['scale'], message: 'Shkala kodini kiriting.' });
        }
        if (Number.isNaN(values.weight) || values.weight <= 0 || values.weight > MAX_WEIGHT) {
          ctx.addIssue({
            code: 'custom',
            path: ['weight'],
            message: `Og'irlik 0 dan katta va ${String(MAX_WEIGHT)} dan kichik bo'lsin.`,
          });
        }
      }

      if (isTextQuestionType(type) && !Number.isNaN(values.maxLength)) {
        if (
          !Number.isInteger(values.maxLength) ||
          values.maxLength < 1 ||
          values.maxLength > MAX_MAX_LENGTH
        ) {
          ctx.addIssue({
            code: 'custom',
            path: ['maxLength'],
            message: `Maksimal uzunlik 1 dan ${String(MAX_MAX_LENGTH)} gacha butun son bo'lsin.`,
          });
        }
      }

      if (type === 'MultiChoice') {
        if (!Number.isNaN(values.minSelections)) {
          if (!Number.isInteger(values.minSelections) || values.minSelections < 0) {
            ctx.addIssue({
              code: 'custom',
              path: ['minSelections'],
              message: "Butun, manfiy bo'lmagan son bo'lsin.",
            });
          }
        }
        if (!Number.isNaN(values.maxSelections)) {
          if (!Number.isInteger(values.maxSelections) || values.maxSelections < 1) {
            ctx.addIssue({
              code: 'custom',
              path: ['maxSelections'],
              message: "Butun, 1 dan katta yoki teng son bo'lsin.",
            });
          }
        }
        if (
          !Number.isNaN(values.minSelections) &&
          !Number.isNaN(values.maxSelections) &&
          values.minSelections > values.maxSelections
        ) {
          ctx.addIssue({
            code: 'custom',
            path: ['maxSelections'],
            message: '"Ko\'pi bilan" "kamida"dan kichik bo\'lmasin.',
          });
        }
      }

      if (
        values.visibility &&
        (values.visibility.conditions.length < 1 || values.visibility.conditions.length > MAX_CONDITIONS)
      ) {
        ctx.addIssue({
          code: 'custom',
          path: ['visibility'],
          message: `Bitta shartda ko'pi bilan ${String(MAX_CONDITIONS)} shart bo'lishi mumkin.`,
        });
      }

      if (isChoiceQuestionType(type)) {
        if (values.options.length < 2) {
          ctx.addIssue({
            code: 'custom',
            path: ['options'],
            message: 'Kamida 2 ta variant kerak.',
          });
        }
        const seen = new Set<number>();
        for (const option of values.options) {
          if (seen.has(option.value)) {
            ctx.addIssue({
              code: 'custom',
              path: ['options'],
              message: "Variant qiymatlari takrorlanmasin — har biri o'ziga xos bo'lsin.",
            });
            break;
          }
          seen.add(option.value);
        }
        if (
          type === 'MultiChoice' &&
          !Number.isNaN(values.maxSelections) &&
          values.maxSelections > values.options.length
        ) {
          ctx.addIssue({
            code: 'custom',
            path: ['maxSelections'],
            message: "\"Ko'pi bilan\" variantlar sonidan katta bo'lmasin.",
          });
        }
      }
    });
}

export type QuestionDialogFormValues = z.infer<ReturnType<typeof createQuestionFormSchema>>;
