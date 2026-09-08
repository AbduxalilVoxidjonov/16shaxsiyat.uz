/**
 * O'zbekiston telefon raqami formatlash — docs/10-frontend-arxitektura.md, 2-bo'lim
 * (`shared/lib/` ro'yxatida `formatPhone`). Ekranda `+998` prefiksi alohida, tahrirlanadigan
 * qism faqat mahalliy 9 ta raqam — maska `(__) ___-__-__` (`docs/11` E-2, `prompts/20`).
 * Backend `+998XXXXXXXXX` kutadi (`StudentRoadMap.Domain.Students.PhoneNumber`).
 */

export const UZ_LOCAL_PHONE_DIGIT_COUNT = 9;

/** Erkin matndan faqat raqamlarni ajratib, ko'pi bilan 9 tasini qaytaradi (mahalliy qism, `+998`siz). */
export function extractUzLocalDigits(raw: string): string {
  return raw.replace(/\D/g, '').slice(0, UZ_LOCAL_PHONE_DIGIT_COUNT);
}

/** Mahalliy raqamlarni `(XX) XXX-XX-XX` mask ko'rinishida, kiritilgan qism bo'yicha formatlaydi. */
export function formatUzLocalDigits(digits: string): string {
  const d = extractUzLocalDigits(digits);
  let out = '';
  if (d.length > 0) out += `(${d.slice(0, 2)}`;
  if (d.length >= 2) out += ')';
  if (d.length > 2) out += ` ${d.slice(2, 5)}`;
  if (d.length > 5) out += `-${d.slice(5, 7)}`;
  if (d.length > 7) out += `-${d.slice(7, 9)}`;
  return out;
}

/** 9 ta mahalliy raqamni backend kutadigan `+998XXXXXXXXX` shakliga o'giradi; to'liq bo'lmasa `null`. */
export function toE164UzPhone(digits: string): string | null {
  const d = extractUzLocalDigits(digits);
  return d.length === UZ_LOCAL_PHONE_DIGIT_COUNT ? `+998${d}` : null;
}

/**
 * Ko'rsatish uchun: `+998901234567` → `+998 (90) 123-45-67`. Mahalliy qism 9 ta raqamdan
 * iborat bo'lmasa (kutilmagan shakl, `null`/`undefined`) — kirgan qiymatni o'zgarishsiz qaytaradi.
 */
export function formatUzPhone(value: string | null | undefined): string {
  if (!value) return value ?? '';
  const local = extractUzLocalDigits(value.replace(/^\+?998/, ''));
  return local.length === UZ_LOCAL_PHONE_DIGIT_COUNT ? `+998 ${formatUzLocalDigits(local)}` : value;
}
