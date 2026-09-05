/**
 * Tug'ilgan sana bilan ishlash — ikki anketa (maktab oqimi `E-2` va ommaviy kabinet)
 * uchun UMUMIY yordamchilar.
 *
 * Ilgari bularning hammasi `features/public-assessment/schemas/registrationSchema.ts`
 * ichida edi. Ommaviy (maktabsiz) anketa yosh chegarasi bilan farq qiladi (6–99 ↔ 6–20),
 * lekin sana HISOBI bir xil bo'lishi shart — nusxa ko'chirilsa ikki forma vaqt o'tib
 * bir-biridan ajralib ketardi (`features/*` bir-birini import qilmaydi — `docs/10` §2).
 */

/** RHF/`<select>` uchun uch qismli sana qiymati (kun/oy/yil satrlari). */
export interface BirthDateValue {
  day: string;
  month: string;
  year: string;
}

/** Kalendarda shunday sana bormi (31-fevral kabi qiymatlarni rad etadi). */
export function isValidCalendarDate(day: number, month: number, year: number): boolean {
  const date = new Date(Date.UTC(year, month - 1, day));
  return (
    date.getUTCFullYear() === year && date.getUTCMonth() === month - 1 && date.getUTCDate() === day
  );
}

/**
 * To'liq yoshni hisoblaydi — backend `StartSessionCommandValidator.IsAgeInRange` va
 * `StartPublicSessionCommandValidator` bilan bir xil algoritm (UTC, tug'ilgan kun
 * kelmagan bo'lsa bir yosh kam).
 */
export function calculateAge(birth: Date, now: Date): number {
  let age = now.getUTCFullYear() - birth.getUTCFullYear();
  const beforeBirthday =
    now.getUTCMonth() < birth.getUTCMonth() ||
    (now.getUTCMonth() === birth.getUTCMonth() && now.getUTCDate() < birth.getUTCDate());
  if (beforeBirthday) {
    age -= 1;
  }
  return age;
}

/** Uch qismli qiymatni backend kutgan `YYYY-MM-DD` (`date`) satriga o'giradi. */
export function birthDateToIso(value: BirthDateValue): string {
  return `${value.year}-${value.month.padStart(2, '0')}-${value.day.padStart(2, '0')}`;
}
