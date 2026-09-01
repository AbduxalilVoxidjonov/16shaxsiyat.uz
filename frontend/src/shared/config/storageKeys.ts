/**
 * `localStorage` kalitlari — CLAUDE.md qoida: faqat sessiya tokeni va javob navbati uchun
 * ishlatiladi (admin auth tokeni uchun EMAS, u xotirada saqlanadi).
 */
export const STORAGE_KEYS = {
  /** `features/public-assessment/store/sessionStore.ts` (Zustand persist) yozadigan kalit. */
  session: 'salohiyat.session',
  /** Yuborilmagan javoblar navbati (offline holatda to'planadi). */
  pendingAnswers: 'salohiyat.pendingAnswers',
} as const;
