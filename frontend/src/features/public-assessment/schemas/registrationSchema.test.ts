import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  REGISTRATION_FORM_DEFAULT_DEFINITION,
  type RegistrationFormCoreFields,
  type RegistrationFormCustomField,
  type RegistrationFormDefinition,
} from '@/shared/api/registrationFormSettingsTypes';
import { birthDateToIso, buildRegistrationSchema, calculateAge } from './registrationSchema';

function validValues(overrides: Record<string, unknown> = {}) {
  return {
    fullName: 'Aliyev Sardor Bekzodovich',
    birthDate: { day: '17', month: '4', year: '2015' },
    gender: 'Male',
    grade: '9',
    classLetter: 'B',
    phone: '901234567',
    parentPhone: '901112233',
    email: '',
    consentAccepted: true,
    accessCode: '',
    customFields: {},
    ...overrides,
  };
}

/** `REGISTRATION_FORM_DEFAULT_DEFINITION.coreFields`ni qisman o'zgartirib qaytaradi. */
function formWithCoreOverrides(
  coreOverrides: Partial<Record<keyof RegistrationFormCoreFields, RegistrationFormCoreFields[keyof RegistrationFormCoreFields]['requirement']>>,
  customFields: RegistrationFormCustomField[] = [],
): RegistrationFormDefinition {
  const coreFields = { ...REGISTRATION_FORM_DEFAULT_DEFINITION.coreFields };
  for (const [key, requirement] of Object.entries(coreOverrides)) {
    const typedKey = key as keyof RegistrationFormCoreFields;
    coreFields[typedKey] = { ...coreFields[typedKey], requirement };
  }
  return { coreFields, customFields };
}

const SHORT_TEXT_FIELD: RegistrationFormCustomField = {
  code: 'PARENT_JOB',
  type: 'ShortText',
  labelUz: 'Ota-onangiz kasbi',
  placeholderUz: null,
  requirement: 'Required',
  maxLength: 200,
  inputPattern: null,
  options: null,
  order: 9,
};

const SINGLE_CHOICE_FIELD: RegistrationFormCustomField = {
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
  order: 9,
};

const MULTI_CHOICE_FIELD: RegistrationFormCustomField = {
  code: 'HOBBIES',
  type: 'MultiChoice',
  labelUz: "Qiziqishlaringiz",
  placeholderUz: null,
  requirement: 'Optional',
  maxLength: null,
  inputPattern: null,
  options: [
    { textUz: 'Sport', value: 'sport', order: 1 },
    { textUz: "San'at", value: 'art', order: 2 },
  ],
  order: 10,
};

describe('calculateAge', () => {
  it("tug'ilgan kun hali kelmagan bo'lsa bir yosh kamroq hisoblaydi", () => {
    const birth = new Date(Date.UTC(2010, 4, 17)); // 17-may-2010
    const now = new Date(Date.UTC(2026, 3, 1)); // 1-aprel-2026 (tug'ilgan kundan oldin)
    expect(calculateAge(birth, now)).toBe(15);
  });

  it("tug'ilgan kun o'tgan bo'lsa to'g'ri yoshni hisoblaydi", () => {
    const birth = new Date(Date.UTC(2010, 4, 17));
    const now = new Date(Date.UTC(2026, 4, 18));
    expect(calculateAge(birth, now)).toBe(16);
  });
});

describe('buildRegistrationSchema', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date(Date.UTC(2026, 8, 2)));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("to'g'ri to'ldirilgan (accessCode shart bo'lmagan) forma o'tadi", () => {
    const schema = buildRegistrationSchema(false);
    const result = schema.safeParse(validValues());
    expect(result.success).toBe(true);
  });

  it('F.I.Sh. 5 belgidan kam bo\'lsa xato beradi', () => {
    const schema = buildRegistrationSchema(false);
    const result = schema.safeParse(validValues({ fullName: 'Ali' }));
    expect(result.success).toBe(false);
  });

  it('6 yoshdan kichik tug\'ilgan sana uchun birthDate.year ostida xato beradi', () => {
    const schema = buildRegistrationSchema(false);
    const result = schema.safeParse(validValues({ birthDate: { day: '1', month: '1', year: '2022' } }));
    expect(result.success).toBe(false);
    if (!result.success) {
      const issue = result.error.issues.find((i) => i.path.join('.') === 'birthDate.year');
      expect(issue?.message).toMatch(/6-20 yosh/);
    }
  });

  it('20 yoshdan katta tug\'ilgan sana uchun ham xato beradi', () => {
    const schema = buildRegistrationSchema(false);
    const result = schema.safeParse(validValues({ birthDate: { day: '1', month: '1', year: '2000' } }));
    expect(result.success).toBe(false);
  });

  it("mavjud bo'lmagan kalendar sanasi (29-fevral, kabisa bo'lmagan yil) birthDate.day ostida xato beradi", () => {
    const schema = buildRegistrationSchema(false);
    const result = schema.safeParse(validValues({ birthDate: { day: '30', month: '2', year: '2015' } }));
    expect(result.success).toBe(false);
    if (!result.success) {
      const issue = result.error.issues.find((i) => i.path.join('.') === 'birthDate.day');
      expect(issue).toBeDefined();
    }
  });

  it("noto'g'ri jins qiymatini rad etadi", () => {
    const schema = buildRegistrationSchema(false);
    const result = schema.safeParse(validValues({ gender: '' }));
    expect(result.success).toBe(false);
  });

  it("to'liq bo'lmagan telefon raqamini rad etadi", () => {
    const schema = buildRegistrationSchema(false);
    const result = schema.safeParse(validValues({ phone: '9012' }));
    expect(result.success).toBe(false);
  });

  // 2026-09-23 egasi qarori: standartda ota-ona telefoni MAJBURIY, o'z telefoni IXTIYORIY.
  it("standartda bo'sh ota-ona telefonini rad etadi ('kiriting' xabari bilan)", () => {
    const schema = buildRegistrationSchema(false);
    const result = schema.safeParse(validValues({ parentPhone: '' }));
    expect(result.success).toBe(false);
    const issue = result.error?.issues.find((i) => i.path[0] === 'parentPhone');
    expect(issue?.message).toMatch(/kiriting\.$/);
    expect(issue?.message).not.toMatch(/bo'sh qoldiring/);
  });

  it("standartda bo'sh shaxsiy telefonni (ixtiyoriy) qabul qiladi", () => {
    const schema = buildRegistrationSchema(false);
    const result = schema.safeParse(validValues({ phone: '' }));
    expect(result.success).toBe(true);
  });

  it("standartda chala shaxsiy telefon 'to'liq kiriting yoki bo'sh qoldiring' xabarini beradi", () => {
    const schema = buildRegistrationSchema(false);
    const result = schema.safeParse(validValues({ phone: '9012' }));
    expect(result.success).toBe(false);
    const issue = result.error?.issues.find((i) => i.path[0] === 'phone');
    expect(issue?.message).toMatch(/bo'sh qoldiring/);
  });

  it("noto'g'ri email formatini rad etadi", () => {
    const schema = buildRegistrationSchema(false);
    const result = schema.safeParse(validValues({ email: 'not-an-email' }));
    expect(result.success).toBe(false);
  });

  it('rozilik belgilanmagan bo\'lsa rad etadi', () => {
    const schema = buildRegistrationSchema(false);
    const result = schema.safeParse(validValues({ consentAccepted: false }));
    expect(result.success).toBe(false);
  });

  it('requiresAccessCode=true bo\'lsa bo\'sh kirish kodini rad etadi', () => {
    const schema = buildRegistrationSchema(true);
    const result = schema.safeParse(validValues({ accessCode: '' }));
    expect(result.success).toBe(false);
  });

  it('requiresAccessCode=true bo\'lsa 6 xonali kodni qabul qiladi', () => {
    const schema = buildRegistrationSchema(true);
    const result = schema.safeParse(validValues({ accessCode: '482913' }));
    expect(result.success).toBe(true);
  });

  it("requiresAccessCode=false bo'lsa bo'sh kirish kodi to'sqinlik qilmaydi", () => {
    const schema = buildRegistrationSchema(false);
    const result = schema.safeParse(validValues({ accessCode: '' }));
    expect(result.success).toBe(true);
  });

  // ── `registrationForm.coreFields` (P52 2-to'lqin, 2026-09-12, `docs/18` §9.6.2) ──────────
  it("registrationForm berilmasa standart (REGISTRATION_FORM_DEFAULT_DEFINITION) xatti-harakat bilan bir xil natija beradi", () => {
    const withDefaultArg = buildRegistrationSchema(false, REGISTRATION_FORM_DEFAULT_DEFINITION);
    const withoutArg = buildRegistrationSchema(false);
    const values = validValues();
    expect(withDefaultArg.safeParse(values).success).toBe(true);
    expect(withoutArg.safeParse(values).success).toBe(true);
    // Standartda `email` bo'sh telefon kabi ixtiyoriy bo'lib qoladi.
    expect(withoutArg.safeParse(validValues({ email: '' })).success).toBe(true);
  });

  it("'email' maydoni 'Required' qilinsa bo'sh qiymatni rad etadi", () => {
    const schema = buildRegistrationSchema(false, formWithCoreOverrides({ email: 'Required' }));
    const result = schema.safeParse(validValues({ email: '' }));
    expect(result.success).toBe(false);
  });

  it("'email' maydoni 'Required' qilinsa to'g'ri email bilan o'tadi", () => {
    const schema = buildRegistrationSchema(false, formWithCoreOverrides({ email: 'Required' }));
    const result = schema.safeParse(validValues({ email: 'ali@example.com' }));
    expect(result.success).toBe(true);
  });

  it("'phone' maydoni 'Optional' qilinsa bo'sh qiymatni qabul qiladi", () => {
    const schema = buildRegistrationSchema(false, formWithCoreOverrides({ phone: 'Optional' }));
    const result = schema.safeParse(validValues({ phone: '' }));
    expect(result.success).toBe(true);
  });

  it("'phone' maydoni 'Hidden' qilinganda ham bo'sh qiymatni qabul qiladi", () => {
    const schema = buildRegistrationSchema(false, formWithCoreOverrides({ phone: 'Hidden' }));
    const result = schema.safeParse(validValues({ phone: '' }));
    expect(result.success).toBe(true);
  });

  it("'gender' maydoni 'Optional' qilinsa bo'sh qiymatni qabul qiladi", () => {
    const schema = buildRegistrationSchema(false, formWithCoreOverrides({ gender: 'Optional' }));
    const result = schema.safeParse(validValues({ gender: '' }));
    expect(result.success).toBe(true);
  });

  it("'grade' maydoni 'Optional' qilinsa bo'sh qiymatni qabul qiladi", () => {
    const schema = buildRegistrationSchema(false, formWithCoreOverrides({ grade: 'Optional' }));
    const result = schema.safeParse(validValues({ grade: '' }));
    expect(result.success).toBe(true);
  });

  it("'birthDate' maydoni 'Optional' qilinsa to'liq bo'sh sanani qabul qiladi", () => {
    const schema = buildRegistrationSchema(false, formWithCoreOverrides({ birthDate: 'Optional' }));
    const result = schema.safeParse(
      validValues({ birthDate: { day: '', month: '', year: '' } }),
    );
    expect(result.success).toBe(true);
  });

  it("'birthDate' maydoni 'Optional' bo'lsa ham qisman to'ldirilgan sanani rad etadi", () => {
    const schema = buildRegistrationSchema(false, formWithCoreOverrides({ birthDate: 'Optional' }));
    const result = schema.safeParse(
      validValues({ birthDate: { day: '17', month: '', year: '' } }),
    );
    expect(result.success).toBe(false);
  });

  it("'birthDate' maydoni 'Optional' bo'lsa ham to'liq to'ldirilgan noto'g'ri sanani rad etadi", () => {
    const schema = buildRegistrationSchema(false, formWithCoreOverrides({ birthDate: 'Optional' }));
    const result = schema.safeParse(
      validValues({ birthDate: { day: '1', month: '1', year: '2000' } }),
    );
    expect(result.success).toBe(false);
  });

  // ── `customFields` (P52 2-to'lqin, 2026-09-12, `docs/18` §9.6.2) — superadmin o'z maydoni ──
  it("majburiy ShortText o'z maydon bo'sh bo'lsa customFields.<kod> ostida xato beradi", () => {
    const schema = buildRegistrationSchema(false, formWithCoreOverrides({}, [SHORT_TEXT_FIELD]));
    const result = schema.safeParse(validValues({ customFields: { PARENT_JOB: '' } }));
    expect(result.success).toBe(false);
    if (!result.success) {
      const issue = result.error.issues.find((i) => i.path.join('.') === 'customFields.PARENT_JOB');
      expect(issue).toBeDefined();
    }
  });

  it("majburiy ShortText o'z maydon to'ldirilsa o'tadi", () => {
    const schema = buildRegistrationSchema(false, formWithCoreOverrides({}, [SHORT_TEXT_FIELD]));
    const result = schema.safeParse(validValues({ customFields: { PARENT_JOB: "O'qituvchi" } }));
    expect(result.success).toBe(true);
  });

  it("ShortText o'z maydon maxLength'dan oshsa xato beradi", () => {
    const schema = buildRegistrationSchema(
      false,
      formWithCoreOverrides({}, [{ ...SHORT_TEXT_FIELD, maxLength: 5 }]),
    );
    const result = schema.safeParse(validValues({ customFields: { PARENT_JOB: 'juda uzun matn' } }));
    expect(result.success).toBe(false);
  });

  it("majburiy SingleChoice tanlanmasa xato beradi", () => {
    const schema = buildRegistrationSchema(false, formWithCoreOverrides({}, [SINGLE_CHOICE_FIELD]));
    const result = schema.safeParse(validValues({ customFields: { TRANSPORT: '' } }));
    expect(result.success).toBe(false);
  });

  it("SingleChoice noto'g'ri variant qiymati bilan xato beradi", () => {
    const schema = buildRegistrationSchema(false, formWithCoreOverrides({}, [SINGLE_CHOICE_FIELD]));
    const result = schema.safeParse(validValues({ customFields: { TRANSPORT: '99' } }));
    expect(result.success).toBe(false);
  });

  it("SingleChoice to'g'ri variant (order) bilan o'tadi", () => {
    const schema = buildRegistrationSchema(false, formWithCoreOverrides({}, [SINGLE_CHOICE_FIELD]));
    const result = schema.safeParse(validValues({ customFields: { TRANSPORT: '2' } }));
    expect(result.success).toBe(true);
  });

  it("ixtiyoriy MultiChoice bo'sh massiv bilan o'tadi", () => {
    const schema = buildRegistrationSchema(false, formWithCoreOverrides({}, [MULTI_CHOICE_FIELD]));
    const result = schema.safeParse(validValues({ customFields: { HOBBIES: [] } }));
    expect(result.success).toBe(true);
  });

  it("MultiChoice noto'g'ri variant qiymati bilan xato beradi", () => {
    const schema = buildRegistrationSchema(false, formWithCoreOverrides({}, [MULTI_CHOICE_FIELD]));
    const result = schema.safeParse(validValues({ customFields: { HOBBIES: ['99'] } }));
    expect(result.success).toBe(false);
  });

  it("MultiChoice to'g'ri variantlar bilan o'tadi", () => {
    const schema = buildRegistrationSchema(false, formWithCoreOverrides({}, [MULTI_CHOICE_FIELD]));
    const result = schema.safeParse(validValues({ customFields: { HOBBIES: ['1', '2'] } }));
    expect(result.success).toBe(true);
  });

  it("'Hidden' o'z maydon majburiy bo'lsa ham bo'sh qiymatni qabul qiladi", () => {
    const schema = buildRegistrationSchema(
      false,
      formWithCoreOverrides({}, [{ ...SHORT_TEXT_FIELD, requirement: 'Hidden' }]),
    );
    const result = schema.safeParse(validValues({ customFields: {} }));
    expect(result.success).toBe(true);
  });
});

describe('birthDateToIso', () => {
  it("kun/oy/yilni backend kutgan YYYY-MM-DD shakliga o'giradi (nol bilan to'ldirib)", () => {
    expect(birthDateToIso({ day: '7', month: '4', year: '2015' })).toBe('2015-04-07');
    expect(birthDateToIso({ day: '17', month: '11', year: '2015' })).toBe('2015-11-17');
  });
});
