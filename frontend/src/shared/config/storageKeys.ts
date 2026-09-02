/**
 * `localStorage` kalitlari — CLAUDE.md qoida: faqat sessiya tokeni va javob navbati uchun
 * ishlatiladi (admin auth tokeni uchun EMAS, u xotirada saqlanadi).
 */
export const STORAGE_KEYS = {
  /** `features/public-assessment/store/sessionStore.ts` (Zustand persist) yozadigan kalit. */
  session: 'salohiyat.session',
  /**
   * O'quvchi javoblarining mahalliy keshi — `features/public-assessment/lib/answerQueue.ts`.
   * Har yozuv `pending` bayrog'iga ega: `true` — hali serverga tasdiqlanmagan (backend
   * yuborish navbati, offline holatda to'planadi), `false` — muvaffaqiyatli saqlangan
   * (sahifa yangilanganda/orqaga qaytganda darhol ko'rsatish uchun saqlanib qoladi).
   */
  pendingAnswers: 'salohiyat.pendingAnswers',
} as const;
