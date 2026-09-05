import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  ageFromFormValue,
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

  it("`ageFromFormValue` to'liq bo'lmagan sanada `null` qaytaradi", () => {
    expect(ageFromFormValue({ day: '', month: '4', year: '1990' }, NOW)).toBeNull();
    expect(ageFromFormValue({ day: '17', month: '4', year: '1990' }, NOW)).toBe(36);
    // Tug'ilgan kun hali kelmagan — bir yosh kam.
    expect(ageFromFormValue({ day: '17', month: '12', year: '1990' }, NOW)).toBe(35);
  });
});
