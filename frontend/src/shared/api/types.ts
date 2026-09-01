/**
 * API bilan bog'liq umumiy tiplar.
 *
 * Backend DTO tiplari `npm run generate:api` orqali `schema.d.ts` ga generatsiya qilinadi
 * (docs/10-frontend-arxitektura.md, 6-bo'lim). Backend endpointlari hali yozilmagani sabab
 * (`prompts/19`) bu skelet bosqichida generatsiya ishga tushirilmagan — `frontend/README.md`
 * ga qarang. Qo'lda yozilgan biznes DTO tiplariga ruxsat yo'q; shu fayl faqat transport
 * qatlami (xato, sahifalash) uchun umumiy tiplarni saqlaydi.
 */

/** Backend `application/problem+json` (RFC 9457) javobi — docs/06-arxitektura.md, 6-bo'lim. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

/** `PagedResult<T>` — admin ro'yxat endpointlari uchun umumiy shakl. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}
