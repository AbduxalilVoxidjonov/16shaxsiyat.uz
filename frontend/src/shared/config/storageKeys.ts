/**
 * `localStorage` kalitlari — CLAUDE.md qoida: faqat sessiya tokeni va javob navbati uchun
 * ishlatiladi (admin auth tokeni uchun EMAS, u xotirada saqlanadi).
 */
export const STORAGE_KEYS = {
  /** `features/public-assessment/store/sessionStore.ts` (Zustand persist) yozadigan kalit. */
  session: 'shaxsiyat.session',
  /**
   * O'quvchi javoblarining mahalliy keshi — `features/public-assessment/lib/answerQueue.ts`.
   * Har yozuv `pending` bayrog'iga ega: `true` — hali serverga tasdiqlanmagan (backend
   * yuborish navbati, offline holatda to'planadi), `false` — muvaffaqiyatli saqlangan
   * (sahifa yangilanganda/orqaga qaytganda darhol ko'rsatish uchun saqlanib qoladi).
   */
  pendingAnswers: 'shaxsiyat.pendingAnswers',
  /**
   * Ommaviy foydalanuvchi (Telegram) sessiyasi BOR-YO'QLIGI haqidagi **belgi** — P47.
   *
   * Bu yerda TOKEN saqlanmaydi: access token faqat xotirada, refresh token esa `httpOnly`
   * cookie'da (`docs/07` §2a.1) — ikkalasi ham `localStorage`ga hech qachon yozilmaydi.
   * Saqlanadigan yagona narsa — `'1'` satri, ya'ni "bu brauzerda kirilgan edi". U shunchaki
   * sahifa yangilanganda `POST /api/auth/telegram/refresh` ni chaqirish kerakmi degan
   * savolga javob beradi: belgisiz har bir anonim tashrifchi (bosh sahifa, metodika…) ham
   * har ochilishda 401 bilan tugaydigan ortiqcha so'rov yuborardi.
   *
   * Belgi soxta bo'lsa ham xavf yo'q: refresh cookie'siz so'rov baribir `401` qaytaradi va
   * foydalanuvchi anonim holatda qoladi.
   */
  publicSessionHint: 'shaxsiyat.publicSession',
} as const;
