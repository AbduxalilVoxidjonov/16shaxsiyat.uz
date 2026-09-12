import { z } from 'zod';
import {
  REGISTRATION_FORM_CHOICE_TYPES,
  REGISTRATION_FORM_CUSTOM_FIELD_TYPES,
  REGISTRATION_FORM_FIELD_REQUIREMENT_VALUES,
  REGISTRATION_FORM_PATTERNABLE_TYPES,
  REGISTRATION_FORM_TEXT_TYPES,
  type RegistrationFormCustomField,
  type RegistrationFormCustomFieldOption,
} from '@/shared/api/registrationFormSettingsTypes';
import { CUSTOM_FIELD_CODE_PATTERN } from './registrationFormDraft';
import { findDuplicateRegistrationOptionValues } from './registrationFieldOptionHelpers';

/**
 * O'z maydon qo'shish/tahrirlash dialogining forma qiymatlari. Backend validatsiyasi bilan
 * bayt-bayt mos (`UpdateRegistrationFormSettingsCommandValidator`,
 * `RegistrationFormDefinition.Validate`, `docs/06` §6) — maqsad: server `400`/`409`
 * qaytarguncha ko'p holatni oldindan ushlash (CLAUDE.md/topshiriq: "serverdan xato kutmasin").
 *
 * `maxLength`/`placeholderUz`/`inputPattern` — matn maydonlari `string` (bo'sh = `null`),
 * RHF bilan ishlashda `number | null` dan ko'ra soddaroq.
 */
export const registrationCustomFieldSchema = z
  .object({
    code: z
      .string()
      .trim()
      .min(1, 'Kod kiritilishi shart.')
      .regex(
        CUSTOM_FIELD_CODE_PATTERN,
        "Kod faqat lotin harf, raqam, '-' va '_' belgilaridan (1-20 ta) iborat bo'lishi mumkin.",
      ),
    type: z.enum(REGISTRATION_FORM_CUSTOM_FIELD_TYPES),
    labelUz: z.string().trim().min(1, "Yorliq kiritilishi shart.").max(200, "Yorliq 200 belgidan oshmasligi kerak."),
    placeholderUz: z.string().max(200, "Placeholder 200 belgidan oshmasligi kerak."),
    requirement: z.enum(REGISTRATION_FORM_FIELD_REQUIREMENT_VALUES),
    maxLength: z.string(),
    inputPattern: z.string().max(200, "Andoza (pattern) 200 belgidan oshmasligi kerak."),
    options: z.array(
      z.object({
        textUz: z.string().trim().max(200, 'Variant matni 200 belgidan oshmasligi kerak.'),
        value: z.string().trim().max(100, 'Variant qiymati 100 belgidan oshmasligi kerak.'),
        order: z.number(),
      }),
    ),
  })
  .superRefine((value, ctx) => {
    if (value.maxLength.trim() !== '') {
      const parsed = Number(value.maxLength);
      if (!Number.isInteger(parsed) || parsed < 1 || parsed > 4000) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: "Belgilar chegarasi 1 dan 4000 gacha butun son bo'lishi kerak.",
          path: ['maxLength'],
        });
      }
    }

    if (value.inputPattern.trim() !== '') {
      try {
        // Faqat kompilyatsiya qilinishini tekshiramiz — natija ishlatilmaydi.
        new RegExp(value.inputPattern);
      } catch {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: "Andoza (pattern) formati noto'g'ri — muntazam ifoda sifatida o'qilmadi.",
          path: ['inputPattern'],
        });
      }
    }

    if (REGISTRATION_FORM_CHOICE_TYPES.includes(value.type)) {
      // Barcha tanlov xatolari BITTA `options` yo'liga (array darajasiga) qo'shiladi —
      // `RegistrationOptionsEditor` alohida maydon ostida emas, umumiy `error` bilan
      // ko'rsatadi (`docs/10` §5.5 naqshi: `OptionsEditor.tsx`da ham shunday).
      if (value.options.length < 2) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Kamida 2 ta tanlov varianti kerak.',
          path: ['options'],
        });
      }
      if (value.options.some((option) => !option.textUz.trim() || !option.value.trim())) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: "Har bir variantda matn va qiymat to'ldirilishi shart.",
          path: ['options'],
        });
      }
      const duplicates = findDuplicateRegistrationOptionValues(value.options);
      if (duplicates.size > 0) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: `Takroriy qiymat(lar): ${[...duplicates].join(', ')}.`,
          path: ['options'],
        });
      }
    }
  });

export type RegistrationCustomFieldFormValues = z.infer<typeof registrationCustomFieldSchema>;

/** Bo'sh (yangi maydon qo'shish) forma qiymatlari. */
export function emptyCustomFieldFormValues(): RegistrationCustomFieldFormValues {
  return {
    code: '',
    type: 'ShortText',
    labelUz: '',
    placeholderUz: '',
    requirement: 'Optional',
    maxLength: '',
    inputPattern: '',
    options: [],
  };
}

/** Mavjud maydonni tahrirlash uchun forma qiymatlariga o'giradi. */
export function customFieldToFormValues(field: RegistrationFormCustomField): RegistrationCustomFieldFormValues {
  return {
    code: field.code,
    type: field.type,
    labelUz: field.labelUz,
    placeholderUz: field.placeholderUz ?? '',
    requirement: field.requirement,
    maxLength: field.maxLength === null ? '' : String(field.maxLength),
    inputPattern: field.inputPattern ?? '',
    options: field.options ?? [],
  };
}

/** Forma qiymatlarini `RegistrationFormCustomField` (API shakli) ga o'giradi. `order` chaqiruvchidan keladi. */
export function formValuesToCustomField(
  values: RegistrationCustomFieldFormValues,
  order: number,
): RegistrationFormCustomField {
  const isText = REGISTRATION_FORM_TEXT_TYPES.includes(values.type);
  const isChoice = REGISTRATION_FORM_CHOICE_TYPES.includes(values.type);
  const supportsPattern = REGISTRATION_FORM_PATTERNABLE_TYPES.includes(values.type);

  return {
    code: values.code.trim(),
    type: values.type,
    labelUz: values.labelUz.trim(),
    placeholderUz: values.placeholderUz.trim() === '' ? null : values.placeholderUz.trim(),
    requirement: values.requirement,
    maxLength: isText && values.maxLength.trim() !== '' ? Number(values.maxLength) : null,
    inputPattern: supportsPattern && values.inputPattern.trim() !== '' ? values.inputPattern.trim() : null,
    options: isChoice
      ? values.options.map((option, index) => ({
          textUz: option.textUz.trim(),
          value: option.value.trim(),
          order: index + 1,
        }))
      : null,
    order,
  };
}

export type { RegistrationFormCustomFieldOption };
