import type { components } from '@/shared/api/schema';

/**
 * `GET /api/me/profile` javobi — saqlangan test anketasi (`docs/07` §5.1a). `PublicUser`
 * (`GET /api/me`, Telegram akkaunti) bilan ARALASHTIRILMAYDI. Qo'lda yozilgan DTO yo'q —
 * `schema.d.ts` dan olinadi (`npm run generate:api`).
 */
export type MyStudentProfile = components['schemas']['MyStudentProfileDto'];

/**
 * `PUT /api/me/profile` tanasi (`docs/07` §5.1b) — `StartPublicSessionRequest` bilan bir xil
 * anketa maydonlari, lekin `languageCode`/`programCode` YO'Q (ular sessiyaga tegishli).
 */
export type UpdateStudentProfileRequestBody = components['schemas']['UpdateStudentProfileRequest'];

/**
 * `DELETE /api/me` tanasi (`docs/07` §5.5, 2026-09-08 kengaytmasi) — ikki qadamli o'chirish
 * oqimining ikkinchi qadamida to'ldiriladi. `reason` — {@link PublicUserDeletionReason}
 * (`shared/config/accountDeletion.ts`, ikkita feature ishlatgani uchun u yerda).
 */
export type DeleteMyAccountRequestBody = components['schemas']['DeleteMyAccountRequest'];
