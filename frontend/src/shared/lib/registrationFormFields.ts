/**
 * GLOBAL ro'yxatdan o'tish formasi (`RegistrationFormDefinition`, `docs/18` §9.6) — o'quvchi
 * ko'radigan ikkala oqim (`features/public-assessment/pages/RegistrationPage.tsx` — maktab,
 * `features/public-account/pages/PublicRegistrationPage.tsx` — Telegram/kabinet) uchun UMUMIY
 * yordamchilar: tartiblash, `customFields` uchun boshlang'ich qiymatlar, zod validatsiyasi va
 * so'rov tanasiga serializatsiya.
 *
 * `shared/` da — `docs/10` §2 "features bir-birini import qilmaydi" qoidasi: ikkala feature
 * ham shu yerdan foydalanadi, biri ikkinchisidan EMAS. `features/settings/model/
 * registrationFormDraft.ts` da o'xshash (lekin ADMIN oldindan ko'rish uchun) funksiyalar bor —
 * ular ATAYLAB qayta ishlatilmagan (feature-cross-import taqiqlangan), bu yerdagi nusxa
 * mustaqil.
 */
import { z } from 'zod';
import {
  REGISTRATION_FORM_CORE_FIELD_KEYS,
  REGISTRATION_FORM_PATTERNABLE_TYPES,
  type RegistrationCustomFieldAnswers,
  type RegistrationFormCoreFieldKey,
  type RegistrationFormCustomField,
  type RegistrationFormDefinition,
  type RegistrationFormFieldRequirement,
} from '@/shared/api/registrationFormSettingsTypes';

export interface OrderedCoreField {
  kind: 'core';
  key: RegistrationFormCoreFieldKey;
  requirement: RegistrationFormFieldRequirement;
  labelUz: string;
  placeholderUz: string | null;
  order: number;
}

export interface OrderedCustomField {
  kind: 'custom';
  field: RegistrationFormCustomField;
  order: number;
}

export type OrderedRegistrationField = OrderedCoreField | OrderedCustomField;

/**
 * Barcha maydonlar (asosiy + o'z) BITTA `order` bo'yicha tartiblangan ro'yxatda — superadmin
 * "Sozlamalar" sahifasida tartibni o'zgartirishi mumkin (`RegistrationCoreFieldsSection`),
 * shu sabab render tartibi HAM shundan hisoblanadi, qattiq yozilmaydi.
 */
export function orderedRegistrationFields(
  definition: RegistrationFormDefinition,
): OrderedRegistrationField[] {
  const core: OrderedCoreField[] = REGISTRATION_FORM_CORE_FIELD_KEYS.map((key) => {
    const field = definition.coreFields[key];
    return {
      kind: 'core',
      key,
      requirement: field.requirement,
      labelUz: field.labelUz,
      placeholderUz: field.placeholderUz,
      order: field.order,
    };
  });
  const custom: OrderedCustomField[] = definition.customFields.map((field) => ({
    kind: 'custom',
    field,
    order: field.order,
  }));
  return [...core, ...custom].sort((a, b) => a.order - b.order);
}

/** `customFields` sub-formasining RHF qiymat shakli — matn turlarida satr, `MultiChoice`da massiv. */
export type CustomFieldFormValue = string | string[];
export type CustomFieldsFormValues = Record<string, CustomFieldFormValue>;

/** Har bir o'z maydon uchun bo'sh boshlang'ich qiymat (matn/`SingleChoice` — `''`, `MultiChoice` — `[]`). */
export function customFieldsDefaultValues(
  customFields: readonly RegistrationFormCustomField[],
): CustomFieldsFormValues {
  const values: CustomFieldsFormValues = {};
  for (const field of customFields) {
    values[field.code] = field.type === 'MultiChoice' ? [] : '';
  }
  return values;
}

/**
 * `customFields` sub-obyekti uchun zod sxemasi — kodlar RUNTIME'da server javobidan keladi
 * (oldindan noma'lum), shu sabab aniq shakl (`z.object` bilan har bir kod uchun alohida
 * validatsiya) o'rniga `z.record` + `superRefine` (`validateCustomFields`) ishlatiladi: bu
 * yondashuv `z.infer` chiqishini barqaror (`Record<string, string | string[]>`) qilib,
 * runtime shartlarni (majburiylik, uzunlik, shablon, variant to'g'riligi) alohida funksiyada
 * tekshiradi.
 */
export const customFieldsRecordSchema = z.record(z.string(), z.union([z.string(), z.array(z.string())]));

/**
 * Bitta o'z maydonning qiymatini tekshiradi va xato bo'lsa `ctx`ga yozadi (`path` — RHF
 * `customFields.<kod>` maydoniga bog'lanadi). `Hidden` maydon — tekshirilmaydi (ko'rsatilmagan,
 * qiymati baribir bo'sh).
 */
export function validateCustomFields(
  fields: readonly RegistrationFormCustomField[],
  values: CustomFieldsFormValues,
  ctx: z.RefinementCtx,
): void {
  for (const field of fields) {
    if (field.requirement === 'Hidden') continue;

    const raw = values[field.code];

    if (field.type === 'MultiChoice') {
      const selected = Array.isArray(raw) ? raw : [];
      if (selected.length === 0) {
        if (field.requirement === 'Required') {
          ctx.addIssue({
            code: 'custom',
            message: `'${field.labelUz}' dan kamida bittasini tanlang.`,
            path: ['customFields', field.code],
          });
        }
        continue;
      }
      const validOrders = new Set((field.options ?? []).map((option) => String(option.order)));
      if (!selected.every((value) => validOrders.has(value))) {
        ctx.addIssue({
          code: 'custom',
          message: `'${field.labelUz}' uchun noto'g'ri variant.`,
          path: ['customFields', field.code],
        });
      }
      continue;
    }

    if (field.type === 'SingleChoice') {
      const selected = typeof raw === 'string' ? raw : '';
      if (selected === '') {
        if (field.requirement === 'Required') {
          ctx.addIssue({
            code: 'custom',
            message: `'${field.labelUz}' maydonini tanlang.`,
            path: ['customFields', field.code],
          });
        }
        continue;
      }
      const validOrders = new Set((field.options ?? []).map((option) => String(option.order)));
      if (!validOrders.has(selected)) {
        ctx.addIssue({
          code: 'custom',
          message: `'${field.labelUz}' uchun noto'g'ri variant.`,
          path: ['customFields', field.code],
        });
      }
      continue;
    }

    // ShortText / LongText / Phone — matn.
    const text = typeof raw === 'string' ? raw.trim() : '';
    if (text === '') {
      if (field.requirement === 'Required') {
        ctx.addIssue({
          code: 'custom',
          message: `'${field.labelUz}' maydoni kiritilishi shart.`,
          path: ['customFields', field.code],
        });
      }
      continue;
    }

    const defaultMaxLength = field.type === 'LongText' ? 2000 : 200;
    const maxLength = field.maxLength ?? defaultMaxLength;
    if (text.length > maxLength) {
      ctx.addIssue({
        code: 'custom',
        message: `'${field.labelUz}' maydoni ${String(maxLength)} belgidan oshmasligi kerak.`,
        path: ['customFields', field.code],
      });
      continue;
    }

    if (
      REGISTRATION_FORM_PATTERNABLE_TYPES.includes(field.type) &&
      field.inputPattern &&
      !safeRegExpTest(field.inputPattern, text)
    ) {
      ctx.addIssue({
        code: 'custom',
        message: `'${field.labelUz}' maydoni uchun matn kutilgan shablonga mos emas.`,
        path: ['customFields', field.code],
      });
    }
  }
}

/** Admin kiritgan shablon buzuq bo'lsa (nazariy jihatdan `PUT /settings` validatsiyasi oldini oladi) forma qulflanib qolmasin. */
function safeRegExpTest(pattern: string, value: string): boolean {
  try {
    return new RegExp(pattern).test(value);
  } catch {
    return true;
  }
}

/**
 * Forma qiymatlarini `POST`/`PUT` so'rov tanasidagi `customFields` shakliga (kod → qiymat)
 * aylantiradi (`docs/07` §1.2). `Hidden` maydon va bo'sh (to'ldirilmagan ixtiyoriy) maydon
 * umuman qo'shilmaydi. Hech narsa yo'q bo'lsa `undefined` (so'rovga `customFields` kaliti
 * umuman qo'shilmasin).
 */
export function serializeCustomFieldsPayload(
  fields: readonly RegistrationFormCustomField[],
  values: CustomFieldsFormValues,
): RegistrationCustomFieldAnswers | undefined {
  const result: RegistrationCustomFieldAnswers = {};

  for (const field of fields) {
    if (field.requirement === 'Hidden') continue;
    const raw = values[field.code];

    if (field.type === 'MultiChoice') {
      const selected = Array.isArray(raw) ? raw : [];
      if (selected.length === 0) continue;
      result[field.code] = selected.map(Number);
      continue;
    }

    if (field.type === 'SingleChoice') {
      const selected = typeof raw === 'string' ? raw : '';
      if (selected === '') continue;
      result[field.code] = Number(selected);
      continue;
    }

    const text = typeof raw === 'string' ? raw.trim() : '';
    if (text === '') continue;
    result[field.code] = text;
  }

  return Object.keys(result).length > 0 ? result : undefined;
}
