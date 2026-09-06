import type { TelegramLoginRequestBody } from '@/shared/api/types';

/**
 * Telegram Login Widget'ning **redirect** oqimi (`docs/07` §2a.1, `docs/08` §2a).
 *
 * Widget `data-auth-url` bilan chizilganda Telegram JS callback CHAQIRMAYDI — u brauzerni
 * bizning manzilimizga qaytaradi va foydalanuvchi ma'lumotini QUERY parametrlarida beradi.
 * Shu sabab bu yerdagi ish faqat bitta: `URLSearchParams` ni `POST /api/auth/telegram`
 * tanasiga aylantirish.
 *
 * ## Nega `data-onauth` emas
 *
 * `data-onauth` atributi Telegram tomonidan **matn sifatida `eval` qilinadi**. Bizning
 * CSP'da `unsafe-eval` ATAYLAB yo'q (`docs/08` 7-bo'lim), shu sabab widget skripti yuklansa
 * ham iframe'ni umuman chiza olmasdi — `/kirish` sahifasida tugma ko'rinmasdi. Redirect
 * oqimi `eval` talab qilmaydi va CSP'ni bo'shatmasdan ishlaydi.
 */

/** Telegram callback'da qaytaradigan barcha parametrlar (`docs/08` §2a). */
export const TELEGRAM_CALLBACK_PARAMS = [
  'id',
  'first_name',
  'last_name',
  'username',
  'photo_url',
  'auth_date',
  'hash',
] as const;

/**
 * Telegram bermasligi mumkin bo'lgan maydonlar. **Yo'q bo'lsa umuman qo'shilmaydi** —
 * bo'sh satr bilan qo'shish `data_check_string` ni buzadi va server `401
 * TELEGRAM_AUTH_INVALID` qaytaradi (`docs/08` §2a, 1-band).
 */
const OPTIONAL_TEXT_PARAMS = ['first_name', 'last_name', 'username', 'photo_url'] as const;

/**
 * Query parametrlaridan `POST /api/auth/telegram` tanasini yig'adi.
 *
 * Parametrlar Telegram bergan holida qoladi: kalitlar `snake_case`, qiymatlar
 * o'zgartirilmaydi (trim/normalizatsiya YO'Q) — imzo aynan shu qiymatlardan hisoblangan.
 * Faqat `id` va `auth_date` satrdan songa o'giriladi, chunki API sxemasi ularni `int64`
 * deb e'lon qilgan; o'girish imzoga ta'sir qilmaydi (server ularni yana satrga qaytaradi).
 *
 * Telegram callback'i emasligi (yoki majburiy maydon yo'qligi) aniqlansa `null` qaytadi va
 * sahifa oddiy kirish sahifasi sifatida ochiladi.
 */
export function readTelegramCallbackParams(
  params: URLSearchParams,
): TelegramLoginRequestBody | null {
  const rawId = params.get('id');
  const rawAuthDate = params.get('auth_date');
  const hash = params.get('hash');

  if (!rawId || !rawAuthDate || !hash) {
    return null;
  }

  const id = Number(rawId);
  const authDate = Number(rawAuthDate);
  if (!Number.isSafeInteger(id) || !Number.isSafeInteger(authDate)) {
    return null;
  }

  const body: TelegramLoginRequestBody = { id, auth_date: authDate, hash };

  for (const key of OPTIONAL_TEXT_PARAMS) {
    const value = params.get(key);
    // `null` — Telegram bu maydonni umuman bermagan (foydalanuvchida yo'q yoki yashirilgan).
    if (value !== null) {
      body[key] = value;
    }
  }

  return body;
}

/**
 * Shaxsiy ma'lumotni (ism, Telegram id, avatar havolasi) manzil satridan va brauzer
 * tarixidan olib tashlaydi — `pushState` EMAS, `replaceState`: yangi yozuv qo'shilsa
 * foydalanuvchi "orqaga" bosib yana o'sha parametrlarga qaytib qolardi.
 *
 * Telegram bilan aloqasi yo'q parametrlar (masalan `returnUrl`) saqlanadi.
 */
export function stripTelegramCallbackParams(): void {
  const url = new URL(window.location.href);
  for (const key of TELEGRAM_CALLBACK_PARAMS) {
    url.searchParams.delete(key);
  }
  window.history.replaceState(window.history.state, '', `${url.pathname}${url.search}${url.hash}`);
}
