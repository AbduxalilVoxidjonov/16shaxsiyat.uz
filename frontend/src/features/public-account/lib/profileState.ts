import type { Gender, StartPublicSessionRequestBody } from '@/shared/api/types';
import { birthDateToIso, calculateAge } from '@/shared/lib/birthDate';
import { extractUzLocalDigits, toE164UzPhone } from '@/shared/lib/formatPhone';
import type { MyStudentProfile } from '../model/types';
import {
  PARENTAL_CONSENT_AGE,
  PUBLIC_REGISTRATION_DEFAULT_VALUES,
  ageFromFormValue,
  type PublicRegistrationFormValues,
} from '../schemas/publicRegistrationSchema';

/**
 * `/kabinet/test` anketasining UCH holati (`docs/07` §5.4, 2026-09-07):
 *
 * | Holat | Qachon | Ekran |
 * |---|---|---|
 * | `new` | profil yo'q (birinchi test) | to'liq forma, F.I.Sh. Telegram ismidan taklif |
 * | `ready` | profil to'liq, rozilik joriy, (voyaga yetmagan bo'lsa) ota-ona roziligi bor | forma YO'Q — karta + "Testni boshlash" |
 * | `edit` | profil bor, lekin rozilik eskirgan / yetishmayotgan maydon / foydalanuvchi "O'zgartirish" bosdi | forma to'ldirilgan holda |
 *
 * Bu mantiq ATAYLAB komponentdan tashqarida — sof funksiya, to'g'ridan-to'g'ri sinaladi.
 */
export type ProfileFormState = 'new' | 'ready' | 'edit';

/** Barcha majburiy shaxsiy maydonlar bazada bormi (server `Student.Create` da talab qiladi, lekin himoya uchun tekshiriladi). */
export function isProfileComplete(profile: MyStudentProfile): boolean {
  return Boolean(profile.fullName && profile.birthDate && profile.gender && profile.phone);
}

/** Roziliknoma qayta so'ralishi kerakmi — yangi profilda har doim, mavjudida faqat eskirgan bo'lsa. */
export function needsConsent(profile: MyStudentProfile): boolean {
  return !profile.hasProfile || !profile.consentCurrent;
}

export function resolveProfileState(profile: MyStudentProfile, forceEdit: boolean): ProfileFormState {
  if (!profile.hasProfile) return 'new';
  if (forceEdit) return 'edit';

  const parentalOk = !profile.isMinor || profile.parentalConsent;
  return isProfileComplete(profile) && profile.consentCurrent && parentalOk ? 'ready' : 'edit';
}

/** `YYYY-MM-DD` → `BirthDateSelect` qiymati (kun/oy `String(n)` — `padStart`siz, select option'lari shunday). */
function isoToBirthDateValue(iso: string): PublicRegistrationFormValues['birthDate'] {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(iso);
  if (!match) return { day: '', month: '', year: '' };
  const [, year, month, day] = match;
  return {
    day: String(Number(day)),
    month: String(Number(month)),
    year: String(Number(year)),
  };
}

/** `+998901234567` → `901234567` (`PhoneField` faqat mahalliy 9 raqam bilan ishlaydi). */
function phoneToLocalDigits(phone: string): string {
  return extractUzLocalDigits(phone.replace(/^\+?998/, ''));
}

/**
 * Profil → forma boshlang'ich qiymatlari. Profil yo'q bo'lsa faqat F.I.Sh. Telegram
 * taklifidan; `consentAccepted` rozilik joriy bo'lsa `true` (blok ko'rsatilmaydi, sxema
 * ham talab qilmaydi), aks holda `false` — foydalanuvchi o'zi belgilaydi.
 */
export function profileToFormValues(profile: MyStudentProfile): PublicRegistrationFormValues {
  if (!profile.hasProfile) {
    return {
      ...PUBLIC_REGISTRATION_DEFAULT_VALUES,
      fullName: profile.suggestedFullName ?? '',
    };
  }

  return {
    fullName: profile.fullName ?? '',
    birthDate: profile.birthDate
      ? isoToBirthDateValue(profile.birthDate)
      : PUBLIC_REGISTRATION_DEFAULT_VALUES.birthDate,
    gender: profile.gender ?? '',
    grade: profile.grade ? String(profile.grade) : '',
    phone: profile.phone ? phoneToLocalDigits(profile.phone) : '',
    email: profile.email ?? '',
    consentAccepted: profile.consentCurrent,
    parentalConsent: profile.parentalConsent,
  };
}

/**
 * Forma qiymatlari → `POST /api/me/sessions` tanasi. Ikki rejim farqi (`docs/07` §5.4):
 *
 * - `new`: hozirgidek to'liq to'plam; `grade: null` = "maktabda o'qimayman", `email: null`.
 * - `edit`: server `null` ni "o'zgarmasin" deb tushunadi, shu sabab bo'sh tanlov ANIQ
 *   yuboriladi — `grade: 0` (`Student.NoGrade`), `email: ''` (tozalash). Rozilik joriy bo'lsa
 *   `consentAccepted` YUBORILMAYDI (aks holda server rozilik sanasini qayta yozardi);
 *   `parentalConsent` faqat voyaga yetmaganda.
 */
export function buildStartSessionPayload(
  values: PublicRegistrationFormValues,
  mode: 'new' | 'edit',
  options: { needsConsent: boolean; programCode?: string },
): StartPublicSessionRequestBody {
  const age = ageFromFormValue(values.birthDate);
  const isMinor = age !== null && age < PARENTAL_CONSENT_AGE;
  const base: StartPublicSessionRequestBody = {
    fullName: values.fullName,
    birthDate: birthDateToIso(values.birthDate),
    gender: values.gender as Gender,
    phone: toE164UzPhone(values.phone) ?? '',
    languageCode: 'uz',
    ...(options.programCode ? { programCode: options.programCode } : {}),
  };

  if (mode === 'new') {
    return {
      ...base,
      consentAccepted: values.consentAccepted,
      parentalConsent: values.parentalConsent,
      // Bo'sh tanlov — "maktabda o'qimayman": `null`, `0` EMAS (`docs/07` §5.4, yangi profil).
      grade: values.grade ? Number(values.grade) : null,
      email: values.email || null,
    };
  }

  return {
    ...base,
    ...(options.needsConsent ? { consentAccepted: values.consentAccepted } : {}),
    ...(isMinor ? { parentalConsent: values.parentalConsent } : {}),
    grade: values.grade ? Number(values.grade) : 0,
    email: values.email,
  };
}

/** `ready` holati: shaxsiy ma'lumot YUBORILMAYDI — faqat dastur (bo'lsa). */
export function buildReadyPayload(programCode?: string): StartPublicSessionRequestBody {
  return programCode ? { programCode } : {};
}

/** Profil sanasidan yosh — karta/ko'rsatish uchun; sana yo'q/buzuq bo'lsa `null`. */
export function profileAge(profile: MyStudentProfile, now: Date = new Date()): number | null {
  if (!profile.birthDate) return null;
  const date = new Date(profile.birthDate);
  return Number.isNaN(date.getTime()) ? null : calculateAge(date, now);
}
