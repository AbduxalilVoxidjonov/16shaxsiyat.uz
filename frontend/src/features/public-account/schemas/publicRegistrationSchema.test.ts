import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  ageFromFormValue,
  createPublicRegistrationSchema,
  publicRegistrationSchema,
  type PublicRegistrationFormValues,
} from './publicRegistrationSchema';

/** Bugungi sana testda qat'iy: 2026-09-05. */
const NOW = new Date('2026-09-05T00:00:00Z');

function values(overrides: Partial<PublicRegistrationFormValues> = {}) {
  return {
    fullName: 'Aliyev Sardor Bekzodovich',
    birthDate: { day: '17', month: '4', year: '1990' },
    gender: 'Male',
    grade: '',
    phone: '901234567',
    email: '',
    consentAccepted: true,
    parentalConsent: false,
    customFields: {},
    ...overrides,
  };
}

function firstIssuePath(result: ReturnType<typeof publicRegistrationSchema.safeParse>): string[] {
  if (result.success) return [];
  return result.error.issues.map((issue) => issue.path.join('.'));
}

describe('publicRegistrationSchema', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(NOW);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("kattalar uchun sinfsiz va ota-ona roziligisiz to'g'ri hisoblanadi", () => {
    expect(publicRegistrationSchema.safeParse(values()).success).toBe(true);
  });

  it('maktab oqimidan farqli: 20 yoshdan katta ham qabul qilinadi (6–99)', () => {
    // 1990 → 36 yosh: maktab anketasida (6–20) rad etilardi.
    expect(publicRegistrationSchema.safeParse(values()).success).toBe(true);
    // 99 yoshdan katta — rad etiladi.
    const tooOld = publicRegistrationSchema.safeParse(
      values({ birthDate: { day: '1', month: '1', year: '1900' } }),
    );
    expect(firstIssuePath(tooOld)).toContain('birthDate.year');
  });

  it('6 yoshdan kichik rad etiladi', () => {
    const result = publicRegistrationSchema.safeParse(
      values({ birthDate: { day: '1', month: '1', year: '2024' }, parentalConsent: true }),
    );
    expect(firstIssuePath(result)).toContain('birthDate.year');
  });

  it('18 yoshgacha ota-ona roziligi SHART', () => {
    const minor = values({
      birthDate: { day: '1', month: '1', year: '2012' }, // 14 yosh
      parentalConsent: false,
    });
    const result = publicRegistrationSchema.safeParse(minor);
    expect(result.success).toBe(false);
    expect(firstIssuePath(result)).toContain('parentalConsent');

    const withConsent = publicRegistrationSchema.safeParse({ ...minor, parentalConsent: true });
    expect(withConsent.success).toBe(true);
  });

  it("18 yosh to'lgan foydalanuvchidan ota-ona roziligi so'ralmaydi", () => {
    const result = publicRegistrationSchema.safeParse(
      values({ birthDate: { day: '1', month: '1', year: '2008' }, parentalConsent: false }),
    );
    expect(result.success).toBe(true);
  });

  it("sinf ixtiyoriy: bo'sh qiymat ham, 1–11 ham qabul qilinadi", () => {
    expect(publicRegistrationSchema.safeParse(values({ grade: '' })).success).toBe(true);
    expect(
      publicRegistrationSchema.safeParse(
        values({
          grade: '9',
          birthDate: { day: '1', month: '1', year: '2012' },
          parentalConsent: true,
        }),
      ).success,
    ).toBe(true);
  });

  it("roziliksiz va telefonsiz o'tmaydi", () => {
    expect(
      firstIssuePath(publicRegistrationSchema.safeParse(values({ consentAccepted: false }))),
    ).toContain('consentAccepted');
    expect(
      firstIssuePath(publicRegistrationSchema.safeParse(values({ phone: '9012345' }))),
    ).toContain('phone');
  });

  it("mavjud bo'lmagan sana rad etiladi", () => {
    const result = publicRegistrationSchema.safeParse(
      values({ birthDate: { day: '31', month: '2', year: '2000' } }),
    );
    expect(firstIssuePath(result)).toContain('birthDate.day');
  });

  describe('createPublicRegistrationSchema — rozilik profil holatiga bog\'liq', () => {
    it("`requireConsent: false` (rozilik joriy): `consentAccepted: false` bilan ham o'tadi", () => {
      const schema = createPublicRegistrationSchema({ requireConsent: false });

      expect(schema.safeParse(values({ consentAccepted: false })).success).toBe(true);
    });

    it("`requireConsent: false` bo'lsa ham qolgan qoidalar (telefon, ota-ona roziligi) saqlanadi", () => {
      const schema = createPublicRegistrationSchema({ requireConsent: false });

      expect(firstIssuePath(schema.safeParse(values({ phone: '9012345' })))).toContain('phone');
      expect(
        firstIssuePath(
          schema.safeParse(
            values({ birthDate: { day: '1', month: '1', year: '2012' }, parentalConsent: false }),
          ),
        ),
      ).toContain('parentalConsent');
    });

    it('`requireConsent: true` — standart `publicRegistrationSchema` bilan bir xil', () => {
      const schema = createPublicRegistrationSchema({ requireConsent: true });

      expect(firstIssuePath(schema.safeParse(values({ consentAccepted: false })))).toContain(
        'consentAccepted',
      );
    });
  });

  // ── `genderRequirement`/`customFields` (P52 2-to'lqin, 2026-09-12, `docs/18` §9.6.2) ──────
  describe('genderRequirement — GLOBAL sozlamaga ergashadi', () => {
    it("standart (berilmasa) — 'Required' bilan bir xil, bo'sh jinsni rad etadi", () => {
      const schema = createPublicRegistrationSchema({ requireConsent: true });
      expect(schema.safeParse(values({ gender: '' })).success).toBe(false);
    });

    it("'Hidden' bo'lsa bo'sh jinsni qabul qiladi", () => {
      const schema = createPublicRegistrationSchema({ requireConsent: true, genderRequirement: 'Hidden' });
      expect(schema.safeParse(values({ gender: '' })).success).toBe(true);
    });

    it("'Optional' bo'lsa bo'sh jinsni qabul qiladi, lekin noto'g'ri qiymatni rad etadi", () => {
      const schema = createPublicRegistrationSchema({ requireConsent: true, genderRequirement: 'Optional' });
      expect(schema.safeParse(values({ gender: '' })).success).toBe(true);
      expect(schema.safeParse(values({ gender: 'Other' })).success).toBe(false);
    });
  });

  describe("customFields — superadmin qo'shgan o'z maydonlari", () => {
    const REQUIRED_FIELD = {
      code: 'PARENT_JOB',
      type: 'ShortText' as const,
      labelUz: 'Ota-onangiz kasbi',
      placeholderUz: null,
      requirement: 'Required' as const,
      maxLength: 200,
      inputPattern: null,
      options: null,
      order: 9,
    };

    it("requireCustomFields: true bo'lsa majburiy maydon bo'sh bo'lganda rad etiladi", () => {
      const schema = createPublicRegistrationSchema({
        requireConsent: true,
        customFields: [REQUIRED_FIELD],
        requireCustomFields: true,
      });
      const result = schema.safeParse(values({ customFields: { PARENT_JOB: '' } }));
      expect(result.success).toBe(false);
    });

    it("requireCustomFields: false bo'lsa (edit/consent) majburiy maydon bo'sh bo'lsa ham o'tadi", () => {
      const schema = createPublicRegistrationSchema({
        requireConsent: true,
        customFields: [REQUIRED_FIELD],
        requireCustomFields: false,
      });
      const result = schema.safeParse(values({ customFields: { PARENT_JOB: '' } }));
      expect(result.success).toBe(true);
    });

    it("requireCustomFields: true bo'lsa ham to'ldirilgan qiymat bilan o'tadi", () => {
      const schema = createPublicRegistrationSchema({
        requireConsent: true,
        customFields: [REQUIRED_FIELD],
        requireCustomFields: true,
      });
      const result = schema.safeParse(values({ customFields: { PARENT_JOB: "O'qituvchi" } }));
      expect(result.success).toBe(true);
    });
  });

  it("`ageFromFormValue` to'liq bo'lmagan sanada `null` qaytaradi", () => {
    expect(ageFromFormValue({ day: '', month: '4', year: '1990' }, NOW)).toBeNull();
    expect(ageFromFormValue({ day: '17', month: '4', year: '1990' }, NOW)).toBe(36);
    // Tug'ilgan kun hali kelmagan — bir yosh kam.
    expect(ageFromFormValue({ day: '17', month: '12', year: '1990' }, NOW)).toBe(35);
  });
});
