/**
 * Ro'yxatdan o'tish formasi konstruktori — sof yordamchi funksiyalar (`docs/07` §3.8,
 * `docs/18` §9.6). `RegistrationFormCard.tsx` va uning bo'lim komponentlari shu yerdagi
 * funksiyalarga tayanadi, holatning o'zi (`draft`) komponentda saqlanadi.
 */
import {
  REGISTRATION_FORM_CORE_FIELD_KEYS,
  type RegistrationFormCoreFieldKey,
  type RegistrationFormCoreFields,
  type RegistrationFormCustomField,
  type RegistrationFormDefinition,
} from '@/shared/api/registrationFormSettingsTypes';

/** Kod qoidasi — `RegistrationFormDefinition.CodePattern` (Domain) bilan bayt-bayt bir xil. */
export const CUSTOM_FIELD_CODE_PATTERN = /^[A-Za-z0-9_-]{1,20}$/;

/** `RegistrationCoreFields.ReservedCodes` — o'z maydon kodi bular bilan to'qnashmasligi kerak. */
export const RESERVED_FIELD_CODES: readonly string[] = REGISTRATION_FORM_CORE_FIELD_KEYS;

export interface CoreFieldEntry {
  key: RegistrationFormCoreFieldKey;
  field: RegistrationFormCoreFields[RegistrationFormCoreFieldKey];
}

/** Asosiy maydonlarni `order` bo'yicha tartiblangan ro'yxat sifatida qaytaradi (UI uchun). */
export function coreFieldEntries(coreFields: RegistrationFormCoreFields): CoreFieldEntry[] {
  return REGISTRATION_FORM_CORE_FIELD_KEYS.map((key) => ({ key, field: coreFields[key] })).sort(
    (a, b) => a.field.order - b.field.order,
  );
}

/** Bitta asosiy maydonni yangilaydi (`labelUz`/`placeholderUz`/`requirement`), qolganlari tegilmaydi. */
export function updateCoreField(
  coreFields: RegistrationFormCoreFields,
  key: RegistrationFormCoreFieldKey,
  patch: Partial<RegistrationFormCoreFields[RegistrationFormCoreFieldKey]>,
): RegistrationFormCoreFields {
  return { ...coreFields, [key]: { ...coreFields[key], ...patch } };
}

/**
 * Asosiy maydonni tartibda bir pog'ona yuqoriga/pastga ko'chiradi — qo'shni ikkita
 * maydonning `order` qiymatini almashtiradi.
 */
export function moveCoreField(
  coreFields: RegistrationFormCoreFields,
  key: RegistrationFormCoreFieldKey,
  direction: -1 | 1,
): RegistrationFormCoreFields {
  const entries = coreFieldEntries(coreFields);
  const index = entries.findIndex((entry) => entry.key === key);
  const targetIndex = index + direction;
  if (index === -1 || targetIndex < 0 || targetIndex >= entries.length) return coreFields;

  const current = entries[index];
  const target = entries[targetIndex];
  if (!current || !target) return coreFields;

  return {
    ...coreFields,
    [current.key]: { ...current.field, order: target.field.order },
    [target.key]: { ...target.field, order: current.field.order },
  };
}

/** Barcha maydonlarning (asosiy + o'z) eng katta `order` qiymati — yangi maydon shundan keyin qo'shiladi. */
export function nextFieldOrder(definition: RegistrationFormDefinition): number {
  const coreMax = Math.max(0, ...Object.values(definition.coreFields).map((field) => field.order));
  const customMax = Math.max(0, ...definition.customFields.map((field) => field.order));
  return Math.max(coreMax, customMax) + 1;
}

/** O'z maydonini `order` bo'yicha tartiblangan holda qaytaradi (UI ro'yxati uchun). */
export function sortedCustomFields(
  customFields: readonly RegistrationFormCustomField[],
): RegistrationFormCustomField[] {
  return [...customFields].sort((a, b) => a.order - b.order);
}

/** O'z maydonni tartibda bir pog'ona ko'chiradi — `moveCoreField` bilan bir xil naqsh. */
export function moveCustomField(
  customFields: readonly RegistrationFormCustomField[],
  code: string,
  direction: -1 | 1,
): RegistrationFormCustomField[] {
  const sorted = sortedCustomFields(customFields);
  const index = sorted.findIndex((field) => field.code === code);
  const targetIndex = index + direction;
  if (index === -1 || targetIndex < 0 || targetIndex >= sorted.length) return customFields as RegistrationFormCustomField[];

  const current = sorted[index];
  const target = sorted[targetIndex];
  if (!current || !target) return customFields as RegistrationFormCustomField[];

  const currentOrder = current.order;
  const targetOrder = target.order;
  return customFields.map((field) => {
    if (field.code === current.code) return { ...field, order: targetOrder };
    if (field.code === target.code) return { ...field, order: currentOrder };
    return field;
  });
}

/**
 * Barcha maydonlar (asosiy + o'z) — jonli oldindan ko'rish uchun BITTA `order` bo'yicha
 * tartiblangan ro'yxat. `kind` diskriminatori preview'da qulflash/tahrirlash belgisini
 * ko'rsatish uchun kerak.
 */
export interface PreviewField {
  kind: 'core' | 'custom';
  key: string;
  labelUz: string;
  placeholderUz: string | null;
  requirement: RegistrationFormCoreFields[RegistrationFormCoreFieldKey]['requirement'];
  order: number;
  type?: RegistrationFormCustomField['type'];
  options?: RegistrationFormCustomField['options'];
}

export function buildPreviewFields(definition: RegistrationFormDefinition): PreviewField[] {
  const core: PreviewField[] = coreFieldEntries(definition.coreFields).map(({ key, field }) => ({
    kind: 'core',
    key,
    labelUz: field.labelUz,
    placeholderUz: field.placeholderUz,
    requirement: field.requirement,
    order: field.order,
  }));
  const custom: PreviewField[] = sortedCustomFields(definition.customFields).map((field) => ({
    kind: 'custom',
    key: field.code,
    labelUz: field.labelUz,
    placeholderUz: field.placeholderUz,
    requirement: field.requirement,
    order: field.order,
    type: field.type,
    options: field.options,
  }));
  return [...core, ...custom].sort((a, b) => a.order - b.order);
}

/** Kod formati — `REGISTRATION_FORM_FIELD_CODE_INVALID` bilan bir xil qoida. */
export function isValidFieldCode(code: string): boolean {
  return CUSTOM_FIELD_CODE_PATTERN.test(code);
}

/**
 * Kod band emasligini tekshiradi — asosiy maydon nomlari bilan yoki (`excludeCode`dan
 * tashqari) boshqa o'z maydon kodi bilan to'qnashmasligi kerak (`REGISTRATION_FORM_FIELD_
 * CODE_DUPLICATE`). Backend `StringComparer.Ordinal` — katta/kichik harf farqlanadi.
 */
export function isFieldCodeTaken(
  code: string,
  customFields: readonly RegistrationFormCustomField[],
  excludeCode?: string,
): boolean {
  if (RESERVED_FIELD_CODES.includes(code)) return true;
  return customFields.some((field) => field.code === code && field.code !== excludeCode);
}
