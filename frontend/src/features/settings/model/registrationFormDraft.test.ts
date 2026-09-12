import { describe, expect, it } from 'vitest';
import {
  REGISTRATION_FORM_DEFAULT_DEFINITION,
  type RegistrationFormCustomField,
} from '@/shared/api/registrationFormSettingsTypes';
import {
  buildPreviewFields,
  coreFieldEntries,
  isFieldCodeTaken,
  isValidFieldCode,
  moveCoreField,
  moveCustomField,
  nextFieldOrder,
  sortedCustomFields,
  updateCoreField,
} from './registrationFormDraft';

const PARENT_JOB: RegistrationFormCustomField = {
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

const TRANSPORT: RegistrationFormCustomField = {
  code: 'TRANSPORT',
  type: 'SingleChoice',
  labelUz: 'Transport turi',
  placeholderUz: null,
  requirement: 'Required',
  maxLength: null,
  inputPattern: null,
  options: [
    { textUz: 'Piyoda', value: 'foot', order: 1 },
    { textUz: 'Avtobus', value: 'bus', order: 2 },
  ],
  order: 10,
};

describe('coreFieldEntries', () => {
  it("`order` bo'yicha tartiblaydi va sakkizta yozuv qaytaradi", () => {
    const entries = coreFieldEntries(REGISTRATION_FORM_DEFAULT_DEFINITION.coreFields);
    expect(entries).toHaveLength(8);
    expect(entries[0]?.key).toBe('fullName');
    expect(entries[7]?.key).toBe('email');
    expect(entries.map((entry) => entry.field.order)).toEqual([1, 2, 3, 4, 5, 6, 7, 8]);
  });
});

describe('updateCoreField', () => {
  it('faqat ko\'rsatilgan maydonni yangilaydi, qolganlari tegilmaydi', () => {
    const next = updateCoreField(REGISTRATION_FORM_DEFAULT_DEFINITION.coreFields, 'fullName', {
      labelUz: 'F.I.Sh. (yangi)',
    });
    expect(next.fullName.labelUz).toBe('F.I.Sh. (yangi)');
    expect(next.fullName.requirement).toBe('Required');
    expect(next.birthDate).toBe(REGISTRATION_FORM_DEFAULT_DEFINITION.coreFields.birthDate);
  });
});

describe('moveCoreField', () => {
  it("ikkita qo'shni maydonning `order`ini almashtiradi", () => {
    const next = moveCoreField(REGISTRATION_FORM_DEFAULT_DEFINITION.coreFields, 'birthDate', -1);
    expect(next.fullName.order).toBe(2);
    expect(next.birthDate.order).toBe(1);
  });

  it('chegaradan tashqariga chiqarishga urinilsa o\'zgarishsiz qaytaradi', () => {
    const coreFields = REGISTRATION_FORM_DEFAULT_DEFINITION.coreFields;
    expect(moveCoreField(coreFields, 'fullName', -1)).toBe(coreFields);
    expect(moveCoreField(coreFields, 'email', 1)).toBe(coreFields);
  });
});

describe('nextFieldOrder', () => {
  it("asosiy va o'z maydonlarning eng katta `order`idan bittaga ko'p qaytaradi", () => {
    expect(nextFieldOrder(REGISTRATION_FORM_DEFAULT_DEFINITION)).toBe(9);
    expect(
      nextFieldOrder({ ...REGISTRATION_FORM_DEFAULT_DEFINITION, customFields: [PARENT_JOB] }),
    ).toBe(10);
  });
});

describe('sortedCustomFields / moveCustomField', () => {
  it("`order` bo'yicha tartiblaydi", () => {
    expect(sortedCustomFields([TRANSPORT, PARENT_JOB]).map((f) => f.code)).toEqual([
      'PARENT_JOB',
      'TRANSPORT',
    ]);
  });

  it("ikkita maydonning `order`ini almashtiradi", () => {
    const next = moveCustomField([PARENT_JOB, TRANSPORT], 'TRANSPORT', -1);
    const parentJob = next.find((f) => f.code === 'PARENT_JOB');
    const transport = next.find((f) => f.code === 'TRANSPORT');
    expect(transport?.order).toBe(9);
    expect(parentJob?.order).toBe(10);
  });

  it('chegaradan tashqariga chiqarishga urinilsa o\'zgarishsiz qaytaradi', () => {
    const fields = [PARENT_JOB, TRANSPORT];
    expect(moveCustomField(fields, 'PARENT_JOB', -1)).toBe(fields);
    expect(moveCustomField(fields, 'TRANSPORT', 1)).toBe(fields);
  });
});

describe('buildPreviewFields', () => {
  it("asosiy va o'z maydonlarni BITTA `order` bo'yicha birlashtiradi", () => {
    const fields = buildPreviewFields({
      ...REGISTRATION_FORM_DEFAULT_DEFINITION,
      customFields: [PARENT_JOB],
    });
    expect(fields).toHaveLength(9);
    expect(fields[8]?.key).toBe('PARENT_JOB');
    expect(fields[8]?.kind).toBe('custom');
    expect(fields[0]?.key).toBe('fullName');
  });
});

describe('isValidFieldCode / isFieldCodeTaken', () => {
  it("kod formatini `RegistrationFormDefinition.CodePattern` bilan bir xil tekshiradi", () => {
    expect(isValidFieldCode('PARENT_JOB')).toBe(true);
    expect(isValidFieldCode('parent-job-2')).toBe(true);
    expect(isValidFieldCode('bad code!')).toBe(false);
    expect(isValidFieldCode('')).toBe(false);
    expect(isValidFieldCode('a'.repeat(21))).toBe(false);
  });

  it('asosiy maydon nomlari bilan to\'qnashishni ANIQLAYDI', () => {
    expect(isFieldCodeTaken('phone', [])).toBe(true);
    expect(isFieldCodeTaken('PHONE', [])).toBe(false); // StringComparer.Ordinal — katta/kichik harf farqlanadi
  });

  it("boshqa o'z maydon kodi bilan to'qnashishni aniqlaydi, o'zini (tahrirlashda) e'tiborsiz qoldiradi", () => {
    expect(isFieldCodeTaken('PARENT_JOB', [PARENT_JOB])).toBe(true);
    expect(isFieldCodeTaken('PARENT_JOB', [PARENT_JOB], 'PARENT_JOB')).toBe(false);
    expect(isFieldCodeTaken('NEW_CODE', [PARENT_JOB])).toBe(false);
  });
});
