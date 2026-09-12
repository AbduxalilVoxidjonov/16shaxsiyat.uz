import { describe, expect, it } from 'vitest';
import type { RegistrationFormCustomField } from '@/shared/api/registrationFormSettingsTypes';
import {
  customFieldToFormValues,
  emptyCustomFieldFormValues,
  formValuesToCustomField,
  registrationCustomFieldSchema,
} from './registrationFieldSchema';

function parse(overrides: Partial<ReturnType<typeof emptyCustomFieldFormValues>> = {}) {
  return registrationCustomFieldSchema.safeParse({
    ...emptyCustomFieldFormValues(),
    labelUz: 'Ota-onangiz kasbi',
    code: 'PARENT_JOB',
    ...overrides,
  });
}

describe('registrationCustomFieldSchema', () => {
  it("to'g'ri to'ldirilgan `ShortText` maydonini qabul qiladi", () => {
    const result = parse();
    expect(result.success).toBe(true);
  });

  it("kod formati noto'g'ri bo'lsa rad etadi (`REGISTRATION_FORM_FIELD_CODE_INVALID` bilan bir xil qoida)", () => {
    const result = parse({ code: 'bad code!' });
    expect(result.success).toBe(false);
  });

  it("bo'sh yorliqni rad etadi", () => {
    const result = parse({ labelUz: '' });
    expect(result.success).toBe(false);
  });

  it("`maxLength` 1..4000 oralig'idan tashqarida bo'lsa rad etadi, bo'sh qiymatni qabul qiladi", () => {
    expect(parse({ maxLength: '0' }).success).toBe(false);
    expect(parse({ maxLength: '4001' }).success).toBe(false);
    expect(parse({ maxLength: '4000' }).success).toBe(true);
    expect(parse({ maxLength: '' }).success).toBe(true);
  });

  it("kompilyatsiya qilinmaydigan `inputPattern`ni rad etadi", () => {
    const result = parse({ inputPattern: '(' });
    expect(result.success).toBe(false);
  });

  it("`SingleChoice`da 2 tadan kam variantni rad etadi (`REGISTRATION_FORM_CHOICE_OPTIONS_INSUFFICIENT`)", () => {
    const result = parse({
      type: 'SingleChoice',
      options: [{ textUz: 'Piyoda', value: 'foot', order: 1 }],
    });
    expect(result.success).toBe(false);
  });

  it("`SingleChoice`da takroriy qiymatni rad etadi (`REGISTRATION_FORM_OPTION_VALUE_DUPLICATE`)", () => {
    const result = parse({
      type: 'SingleChoice',
      options: [
        { textUz: 'Piyoda', value: 'foot', order: 1 },
        { textUz: 'Avtobus', value: 'foot', order: 2 },
      ],
    });
    expect(result.success).toBe(false);
  });

  it('`SingleChoice`da 2 ta to\'g\'ri variantni qabul qiladi', () => {
    const result = parse({
      type: 'SingleChoice',
      options: [
        { textUz: 'Piyoda', value: 'foot', order: 1 },
        { textUz: 'Avtobus', value: 'bus', order: 2 },
      ],
    });
    expect(result.success).toBe(true);
  });
});

describe('formValuesToCustomField / customFieldToFormValues', () => {
  it("matn turi uchun `options`ni `null`ga, tanlov turi uchun `maxLength`/`inputPattern`ni `null`ga tushiradi", () => {
    const textField = formValuesToCustomField(
      {
        ...emptyCustomFieldFormValues(),
        code: 'PARENT_JOB',
        labelUz: 'Ota-onangiz kasbi',
        type: 'ShortText',
        maxLength: '200',
        options: [{ textUz: 'ignored', value: 'ignored', order: 1 }],
      },
      9,
    );
    expect(textField.maxLength).toBe(200);
    expect(textField.options).toBeNull();

    const choiceField = formValuesToCustomField(
      {
        ...emptyCustomFieldFormValues(),
        code: 'TRANSPORT',
        labelUz: 'Transport turi',
        type: 'SingleChoice',
        maxLength: '200',
        inputPattern: '^[0-9]+$',
        options: [
          { textUz: 'Piyoda', value: 'foot', order: 5 },
          { textUz: 'Avtobus', value: 'bus', order: 9 },
        ],
      },
      10,
    );
    expect(choiceField.maxLength).toBeNull();
    expect(choiceField.inputPattern).toBeNull();
    expect(choiceField.options).toEqual([
      { textUz: 'Piyoda', value: 'foot', order: 1 },
      { textUz: 'Avtobus', value: 'bus', order: 2 },
    ]);
  });

  it("`LongText`da `inputPattern` qo'llab-quvvatlanmaydi (docs/07 §3.8)", () => {
    const field = formValuesToCustomField(
      {
        ...emptyCustomFieldFormValues(),
        code: 'BIO',
        labelUz: 'Qisqacha tarjimai hol',
        type: 'LongText',
        maxLength: '2000',
        inputPattern: '^[0-9]+$',
      },
      9,
    );
    expect(field.maxLength).toBe(2000);
    expect(field.inputPattern).toBeNull();
  });

  it("bo'sh `placeholderUz`ni `null`ga o'giradi, round-trip qiladi", () => {
    const field: RegistrationFormCustomField = {
      code: 'PARENT_JOB',
      type: 'ShortText',
      labelUz: 'Ota-onangiz kasbi',
      placeholderUz: null,
      requirement: 'Optional',
      maxLength: 200,
      inputPattern: null,
      options: null,
      order: 9,
    };
    const values = customFieldToFormValues(field);
    expect(values.placeholderUz).toBe('');
    expect(formValuesToCustomField(values, 9)).toEqual(field);
  });
});
