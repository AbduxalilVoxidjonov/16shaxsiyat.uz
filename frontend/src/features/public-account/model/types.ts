import type { components } from '@/shared/api/schema';

/**
 * `GET /api/me/profile` javobi — saqlangan test anketasi (`docs/07` §5.1a). `PublicUser`
 * (`GET /api/me`, Telegram akkaunti) bilan ARALASHTIRILMAYDI. Qo'lda yozilgan DTO yo'q —
 * `schema.d.ts` dan olinadi (`npm run generate:api`).
 */
export type MyStudentProfile = components['schemas']['MyStudentProfileDto'];
