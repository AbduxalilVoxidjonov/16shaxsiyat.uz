import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Schemas } from '@/test/apiMock';
import {
  buildProfilePayload,
  buildReadyPayload,
  buildStartSessionPayload,
  isProfileComplete,
  needsConsent,
  profileToFormValues,
  resolveProfileState,
  submitActionFor,
  type ProfileFormMode,
} from './profileState';
import type { PublicRegistrationFormValues } from '../schemas/publicRegistrationSchema';

/** Bugungi sana testda qat'iy: 2026-09-07. */
const NOW = new Date('2026-09-07T00:00:00Z');

const NO_PROFILE = {
  hasProfile: false,
  fullName: null,
  birthDate: null,
  phone: null,
  grade: null,
  email: null,
  consentVersion: null,
  consentCurrent: false,
  parentalConsent: false,
  isMinor: false,
  suggestedFullName: 'Valiyev Ali',
} satisfies Schemas['MyStudentProfileDto'];

const FULL_PROFILE = {
  hasProfile: true,
  fullName: 'Karimov Sardor Alisherovich',
  birthDate: '1995-04-12',
  gender: 'Male',
  phone: '+998901234567',
  grade: null,
  email: null,
  consentVersion: '1.0',
  consentCurrent: true,
  parentalConsent: false,
  isMinor: false,
  suggestedFullName: 'Valiyev Ali',
} satisfies Schemas['MyStudentProfileDto'];

const ADULT_VALUES: PublicRegistrationFormValues = {
  fullName: 'Karimov Sardor Alisherovich',
  birthDate: { day: '12', month: '4', year: '1995' },
  gender: 'Male',
  grade: '',
  phone: '901234567',
  email: '',
  consentAccepted: true,
  parentalConsent: false,
};

describe('resolveProfileState (A/B/C/D holatlari)', () => {
  it("A: profil yo'q → `new`, `?edit=1` bo'lsa ham", () => {
    expect(resolveProfileState(NO_PROFILE, false)).toBe('new');
    expect(resolveProfileState(NO_PROFILE, true)).toBe('new');
  });

  it("B: profil to'liq va rozilik joriy → `ready`", () => {
    expect(resolveProfileState(FULL_PROFILE, false)).toBe('ready');
  });

  it("B → D: \"O'zgartirish\" bosilganda (`forceEdit`) forma → `edit` (faqat saqlash)", () => {
    expect(resolveProfileState(FULL_PROFILE, true)).toBe('edit');
  });

  it("D: rozilik eskirgan bo'lsa ham `forceEdit` → `edit` — foydalanuvchi niyati ustun", () => {
    expect(resolveProfileState({ ...FULL_PROFILE, consentCurrent: false }, true)).toBe('edit');
  });

  it('C: rozilik eskirgan → `consent` (`edit` EMAS — bu holatda test boshlanadi)', () => {
    expect(resolveProfileState({ ...FULL_PROFILE, consentCurrent: false }, false)).toBe('consent');
  });

  it("C: voyaga yetmagan, ota-ona roziligi yo'q → `consent`; bor bo'lsa → `ready`", () => {
    const minor = { ...FULL_PROFILE, birthDate: '2012-01-01', isMinor: true };
    expect(resolveProfileState({ ...minor, parentalConsent: false }, false)).toBe('consent');
    expect(resolveProfileState({ ...minor, parentalConsent: true }, false)).toBe('ready');
  });

  it("C: majburiy maydon yetishmasa → `consent` (server bunday holatni bermaydi, lekin himoya)", () => {
    expect(isProfileComplete({ ...FULL_PROFILE, phone: null })).toBe(false);
    expect(resolveProfileState({ ...FULL_PROFILE, phone: null }, false)).toBe('consent');
  });

  it("rozilik: yangi profilda har doim, mavjudida faqat eskirgan bo'lsa so'raladi", () => {
    expect(needsConsent(NO_PROFILE)).toBe(true);
    expect(needsConsent(FULL_PROFILE)).toBe(false);
    expect(needsConsent({ ...FULL_PROFILE, consentCurrent: false })).toBe(true);
  });
});

/**
 * Egasi ko'rgan xatoning QULFI: "O'zgartirish" (`edit`) hech qachon sessiya ochmaydi.
 * Bu qoida o'zgarsa — shu test qizaradi.
 */
describe('submitActionFor (rejim → amal qoidasi)', () => {
  it("`edit` → FAQAT saqlash (`PUT /api/me/profile`)", () => {
    expect(submitActionFor('edit')).toBe('saveProfile');
  });

  it.each<ProfileFormMode>(['new', 'consent'])(
    '`%s` → sessiya (`POST /api/me/sessions`) — foydalanuvchi test boshlamoqchi',
    (mode) => {
      expect(submitActionFor(mode)).toBe('startSession');
    },
  );
});

describe('profileToFormValues', () => {
  it("profil yo'q: F.I.Sh. Telegram taklifidan, qolgani bo'sh", () => {
    const values = profileToFormValues(NO_PROFILE);

    expect(values.fullName).toBe('Valiyev Ali');
    expect(values.birthDate).toEqual({ day: '', month: '', year: '' });
    expect(values.phone).toBe('');
    expect(values.consentAccepted).toBe(false);
  });

  it("profil yo'q va Telegram ismi ham yo'q: F.I.Sh. bo'sh", () => {
    expect(profileToFormValues({ ...NO_PROFILE, suggestedFullName: null }).fullName).toBe('');
  });

  it('profil bor: hamma maydon forma shakliga o\'giriladi (sana `String(n)`, telefon 9 raqam)', () => {
    const values = profileToFormValues({
      ...FULL_PROFILE,
      grade: 9,
      email: 'sardor@example.com',
      birthDate: '2007-01-05',
    });

    expect(values.fullName).toBe('Karimov Sardor Alisherovich');
    // `BirthDateSelect` option qiymatlari `padStart`siz — "05" emas, "5".
    expect(values.birthDate).toEqual({ day: '5', month: '1', year: '2007' });
    expect(values.gender).toBe('Male');
    expect(values.grade).toBe('9');
    expect(values.phone).toBe('901234567');
    expect(values.email).toBe('sardor@example.com');
    // Rozilik joriy — blok ko'rsatilmaydi, qiymat `true` bo'lib turadi.
    expect(values.consentAccepted).toBe(true);
  });

  it("`grade: null` → bo'sh tanlov (\"maktabda o'qimayman\")", () => {
    expect(profileToFormValues(FULL_PROFILE).grade).toBe('');
  });
});

describe('buildStartSessionPayload', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(NOW);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("`new`: to'liq to'plam — `grade: null`, `email: null`, rozilik va ota-ona bayroqlari", () => {
    expect(buildStartSessionPayload(ADULT_VALUES, 'new', { needsConsent: true })).toEqual({
      fullName: 'Karimov Sardor Alisherovich',
      birthDate: '1995-04-12',
      gender: 'Male',
      phone: '+998901234567',
      consentAccepted: true,
      parentalConsent: false,
      grade: null,
      email: null,
      languageCode: 'uz',
    });
  });

  it("`edit` + rozilik joriy: `consentAccepted` YUBORILMAYDI, `grade: 0`, `email: ''`", () => {
    const payload = buildStartSessionPayload(ADULT_VALUES, 'edit', { needsConsent: false });

    expect(payload).not.toHaveProperty('consentAccepted');
    expect(payload).not.toHaveProperty('parentalConsent');
    // Server `null` ni "o'zgarmasin" deb tushunadi — bo'sh tanlov ANIQ yuboriladi.
    expect(payload.grade).toBe(0);
    expect(payload.email).toBe('');
    expect(payload.fullName).toBe('Karimov Sardor Alisherovich');
  });

  it('`edit` + rozilik eskirgan: `consentAccepted` yuboriladi', () => {
    const payload = buildStartSessionPayload(ADULT_VALUES, 'edit', { needsConsent: true });

    expect(payload.consentAccepted).toBe(true);
  });

  it('`edit` + voyaga yetmagan: `parentalConsent` yuboriladi', () => {
    const minorValues: PublicRegistrationFormValues = {
      ...ADULT_VALUES,
      birthDate: { day: '1', month: '1', year: '2012' },
      grade: '9',
      parentalConsent: true,
    };

    const payload = buildStartSessionPayload(minorValues, 'edit', { needsConsent: false });

    expect(payload.parentalConsent).toBe(true);
    expect(payload.grade).toBe(9);
  });

  it("`programCode` faqat berilganda qo'shiladi", () => {
    expect(buildStartSessionPayload(ADULT_VALUES, 'new', { needsConsent: true })).not.toHaveProperty(
      'programCode',
    );
    expect(
      buildStartSessionPayload(ADULT_VALUES, 'new', { needsConsent: true, programCode: 'P1' })
        .programCode,
    ).toBe('P1');
  });

  it('`consent` rejimi tanasi `edit` bilan bir xil shaklda (profil bor semantikasi)', () => {
    expect(buildStartSessionPayload(ADULT_VALUES, 'consent', { needsConsent: true })).toEqual(
      buildStartSessionPayload(ADULT_VALUES, 'edit', { needsConsent: true }),
    );
  });

  it("sessiya tanasi = profil tanasi (`buildProfilePayload`) + `languageCode`/`programCode`", () => {
    const profilePayload = buildProfilePayload(ADULT_VALUES, 'edit', { needsConsent: false });

    expect(profilePayload).not.toHaveProperty('languageCode');
    expect(profilePayload).not.toHaveProperty('programCode');
    expect(
      buildStartSessionPayload(ADULT_VALUES, 'edit', { needsConsent: false, programCode: 'P1' }),
    ).toEqual({ ...profilePayload, languageCode: 'uz', programCode: 'P1' });
  });

  it("`ready`: shaxsiy ma'lumot yuborilmaydi — `{}` yoki faqat `programCode`", () => {
    expect(buildReadyPayload()).toEqual({});
    expect(buildReadyPayload('P1')).toEqual({ programCode: 'P1' });
  });
});
