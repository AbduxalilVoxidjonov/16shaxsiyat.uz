/**
 * Muhit o'zgaruvchilari — docs/10-frontend-arxitektura.md, 8-bo'lim.
 * Barcha `import.meta.env` murojaatlari shu faylda markazlashtiriladi.
 */
function readEnv(key: string, fallback = ''): string {
  const value = import.meta.env[key];
  return typeof value === 'string' && value.length > 0 ? value : fallback;
}

export const env = {
  /**
   * API manzili. **Standart qiymat — bo'sh satr, ya'ni "same-origin"**: so'rovlar
   * `/api/...` nisbiy yo'li bilan ketadi va ilova qaysi domenda ochilgan bo'lsa,
   * o'shanda qoladi. Konteynerda (`docker compose`) va dev serverda ikkalasida ham
   * `/api` backendga proksilanadi, shuning uchun sozlama umuman kerak emas.
   *
   * Ilgari bu yerda `http://localhost:5000` qotirilgan edi. Bu jimgina buziladigan
   * xato manbai edi: HTTPS domenda ochilgan ilova `http://localhost:5000` ga murojaat
   * qilib, brauzer uni **mixed content** sifatida bloklardi — so'rov serverga umuman
   * yetib bormasdi va foydalanuvchi faqat umumiy "xatolik" xabarini ko'rardi
   * (2026-09-02 da namoyish paytida aynan shu yuz berdi).
   *
   * Boshqa domendagi backendga ulanish kerak bo'lsa — `VITE_API_BASE_URL` ni aniq bering.
   */
  apiBaseUrl: readEnv('VITE_API_BASE_URL'),
  appName: readEnv('VITE_APP_NAME', 'Salohiyat'),
  sentryDsn: readEnv('VITE_SENTRY_DSN'),
} as const;
