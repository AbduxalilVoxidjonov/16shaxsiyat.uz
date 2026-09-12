/** `features/settings` uchun TanStack Query kalitlari — docs/10, 5.2-bo'lim konvensiyasi. */
export const SETTINGS_QUERY_KEYS = {
  /** `GET /api/auth/me` — faqat 2FA holatini ko'rsatish uchun (`features/settings` o'z nusxasi,
   * `features/auth`dan mustaqil — "features/* bir-birini import qilmaydi" qoidasi, docs/10 2-bo'lim). */
  account: () => ['settings', 'account'] as const,
  /** `GET /api/admin/settings/registration-form` — docs/07 §3.8, docs/10 §5.5. */
  registrationForm: () => ['settings', 'registrationForm'] as const,
};
