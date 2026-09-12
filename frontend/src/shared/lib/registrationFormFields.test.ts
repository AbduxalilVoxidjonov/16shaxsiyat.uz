import { describe, expect, it } from 'vitest';
import { z } from 'zod';
import {
  REGISTRATION_FORM_DEFAULT_DEFINITION,
  type RegistrationFormCustomField,
  type RegistrationFormDefinition,
} from '@/shared/api/registrationFormSettingsTypes';
import {
  customFieldsDefaultValues,
  orderedRegistrationFields,
  serializeCustomFieldsPayload,
  validateCustomFields,
} from './registrationFormFields';

const SHORT_TEXT: RegistrationFormCustomField = {
  code: 'PARENT_JOB',
  type: 'ShortText',
  labelUz: 'Ota-onangiz kasbi',
  placeholderUz: null,
  requirement: 'Required',
  maxLength: 10,
  inputPattern: null,
  options: null,
  order: 9,
};

const LONG_TEXT: RegistrationFormCustomField = {
  code: 'ABOUT',
  type: 'LongText',
  labelUz: "O'zingiz haqingizda",
  placeholderUz: null,
  requirement: 'Optional',
  maxLength: null,
  inputPattern: null,
  options: null,
  order: 10,
};

const SINGLE_CHOICE: RegistrationFormCustomField = {
  code: 'TRANSPORT',
  type: 'SingleChoice',
  labelUz: 'Maktabga qanday borasiz?',
  placeholderUz: null,
  requirement: 'Required',
  maxLength: null,
  inputPattern: null,
  options: [
    { textUz: 'Piyoda', value: 'foot', order: 1 },
    { textUz: 'Avtobus', value: 'bus', order: 2 },
  ],
  order: 11,
};

const MULTI_CHOICE: RegistrationFormCustomField = {
  code: 'HOBBIES',
  type: 'MultiChoice',
  labelUz: 'Qiziqishlar',
  placeholderUz: null,
  requirement: 'Optional',
  maxLength: null,
  inputPattern: null,
  options: [
    { textUz: 'Sport', value: 'sport', order: 1 },
    { textUz: "San'at", value: 'art', order: 2 },
  ],
  order: 12,
};

function definitionWith(customFields: RegistrationFormCustomField[]): RegistrationFormDefinition {
  return { ...REGISTRATION_FORM_DEFAULT_DEFINITION, customFields };
}

function runValidate(fields: RegistrationFormCustomField[], values: Record<string, string | string[]>) {
  const schema = z.object({}).superRefine((_v, ctx) => {
    validateCustomFields(fields, values, ctx);
  });
  return schema.safeParse({});
}

describe('orderedRegistrationFields', () => {
  it("standart ta'rifda 8 ta asosiy maydonni order bo'yicha qaytaradi (customFields bo'sh)", () => {
    const items = orderedRegistrationFields(REGISTRATION_FORM_DEFAULT_DEFINITION);
    expect(items).toHaveLength(8);
    expect(items.every((item) => item.kind === 'core')).toBe(true);
    expect(items.map((i) => i.order)).toEqual([1, 2, 3, 4, 5, 6, 7, 8]);
  });

  it("o'z maydonlar asosiy maydonlar bilan BITTA order ro'yxatida aralashadi", () => {
    const definition = definitionWith([{ ...SHORT_TEXT, order: 1.5 }]);
    const items = orderedRegistrationFields(definition);
    expect(items).toHaveLength(9);
    expect(items[0]?.kind).toBe('core'); // fullName, order 1
    expect(items[1]?.kind).toBe('custom'); // PARENT_JOB, order 1.5
    expect(items[2]?.kind).toBe('core'); // birthDate, order 2
  });

  it("bir nechta o'z maydon ham order bo'yicha tartiblanadi", () => {
    const definition = definitionWith([
      { ...MULTI_CHOICE, order: 100 },
      { ...SHORT_TEXT, order: 50 },
    ]);
    const items = orderedRegistrationFields(definition);
    const customCodes = items.filter((i) => i.kind === 'custom').map((i) => (i.kind === 'custom' ? i.field.code : ''));
    expect(customCodes).toEqual(['PARENT_JOB', 'HOBBIES']);
  });
});

describe('customFieldsDefaultValues', () => {
  it("matn/SingleChoice uchun bo'sh satr, MultiChoice uchun bo'sh massiv", () => {
    const values = customFieldsDefaultValues([SHORT_TEXT, LONG_TEXT, SINGLE_CHOICE, MULTI_CHOICE]);
    expect(values).toEqual({
      PARENT_JOB: '',
      ABOUT: '',
      TRANSPORT: '',
      HOBBIES: [],
    });
  });

  it("maydon yo'q bo'lsa bo'sh obyekt qaytaradi", () => {
    expect(customFieldsDefaultValues([])).toEqual({});
  });
});

describe('validateCustomFields', () => {
  it("majburiy ShortText bo'sh bo'lsa xato beradi", () => {
    const result = runValidate([SHORT_TEXT], { PARENT_JOB: '' });
    expect(result.success).toBe(false);
  });

  it('ShortText maxLengthdan oshsa xato beradi', () => {
    const result = runValidate([SHORT_TEXT], { PARENT_JOB: 'juda-uzun-matn-shu-yerda' });
    expect(result.success).toBe(false);
  });

  it("ixtiyoriy LongText bo'sh bo'lsa o'tadi", () => {
    const result = runValidate([LONG_TEXT], { ABOUT: '' });
    expect(result.success).toBe(true);
  });

  it("majburiy SingleChoice tanlanmasa xato, to'g'ri order bilan o'tadi", () => {
    expect(runValidate([SINGLE_CHOICE], { TRANSPORT: '' }).success).toBe(false);
    expect(runValidate([SINGLE_CHOICE], { TRANSPORT: '99' }).success).toBe(false);
    expect(runValidate([SINGLE_CHOICE], { TRANSPORT: '2' }).success).toBe(true);
  });

  it("ixtiyoriy MultiChoice bo'sh massiv bilan o'tadi, noto'g'ri variant bilan rad etadi", () => {
    expect(runValidate([MULTI_CHOICE], { HOBBIES: [] }).success).toBe(true);
    expect(runValidate([MULTI_CHOICE], { HOBBIES: ['99'] }).success).toBe(false);
    expect(runValidate([MULTI_CHOICE], { HOBBIES: ['1', '2'] }).success).toBe(true);
  });

  it("'Hidden' maydon hech qachon tekshirilmaydi (majburiy bo'lsa ham)", () => {
    const hidden = { ...SHORT_TEXT, requirement: 'Hidden' as const };
    expect(runValidate([hidden], { PARENT_JOB: '' }).success).toBe(true);
  });

  it("inputPattern mos kelmasa xato beradi (ShortText/Phone)", () => {
    const withPattern = { ...SHORT_TEXT, inputPattern: '^[0-9]+$', maxLength: 20 };
    expect(runValidate([withPattern], { PARENT_JOB: 'abc' }).success).toBe(false);
    expect(runValidate([withPattern], { PARENT_JOB: '12345' }).success).toBe(true);
  });
});

describe('serializeCustomFieldsPayload', () => {
  it("matn maydonlarini trim qilib qaytaradi, bo'sh bo'lsa qo'shmaydi", () => {
    const payload = serializeCustomFieldsPayload([SHORT_TEXT, LONG_TEXT], {
      PARENT_JOB: '  Ustoz  ',
      ABOUT: '',
    });
    expect(payload).toEqual({ PARENT_JOB: 'Ustoz' });
  });

  it("SingleChoice qiymatini butun songa (order) o'giradi", () => {
    const payload = serializeCustomFieldsPayload([SINGLE_CHOICE], { TRANSPORT: '2' });
    expect(payload).toEqual({ TRANSPORT: 2 });
  });

  it("MultiChoice qiymatlarini butun sonlar massiviga o'giradi", () => {
    const payload = serializeCustomFieldsPayload([MULTI_CHOICE], { HOBBIES: ['1', '2'] });
    expect(payload).toEqual({ HOBBIES: [1, 2] });
  });

  it("'Hidden' maydon natijaga umuman qo'shilmaydi", () => {
    const hidden = { ...SHORT_TEXT, requirement: 'Hidden' as const };
    const payload = serializeCustomFieldsPayload([hidden], { PARENT_JOB: 'qiymat' });
    expect(payload).toBeUndefined();
  });

  it("hech qanday qiymat bo'lmasa `undefined` qaytaradi (so'rovga kalit qo'shilmaydi)", () => {
    expect(serializeCustomFieldsPayload([LONG_TEXT, MULTI_CHOICE], { ABOUT: '', HOBBIES: [] })).toBeUndefined();
  });
});
