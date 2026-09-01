/**
 * API bilan bog'liq umumiy tiplar.
 *
 * Backend DTO tiplari `npm run generate:api` orqali `schema.d.ts` ga generatsiya qilinadi
 * (docs/10-frontend-arxitektura.md, 6-bo'lim). `ProblemDetails`/`PagedResult<T>` bu yerda
 * qo'lda qoladi — ular **transport qatlami** tiplari (`AppError`, sahifalash konvensiyasi),
 * biznes DTO emas, shu sabab docs/10 6-bo'limdagi "faqat generatsiya" qoidasi ularga tegishli
 * emas. Backend generatsiya qilingan `components["schemas"]["ProblemDetails"]` esa Swashbuckle
 * ning o'z ProblemDetails vakili — u yerda `code`/`traceId`/`errors` kengaytmalari indeks
 * signature (`[key: string]: unknown`) ortida yashiringan, shu sabab bu yerdagi aniq shakl
 * ishlatiladi (pastga qarang).
 */
import type { components } from './schema';

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

/**
 * Ommaviy (o'quvchi) oqimi DTO'lari — docs/07-api-shartnoma.md, 1.1–1.3-bo'lim (`prompts/20`).
 * `npm run generate:api` (backend `localhost:5402`da ko'tarilib sinaldi, `README.md`ga qarang)
 * barcha 6 ommaviy endpoint uchun to'liq javob sxemalarini chiqaradi
 * (`PublicSessionController`ga `[ProducesResponseType]` qo'shilgach) — quyidagilar
 * `schema.d.ts`dagi `components["schemas"]`dan re-export, qo'lda yozilgan nusxa YO'Q.
 *
 * **`required` ro'yxati** (backend `SupportNonNullableReferenceTypes()` +
 * `RequiredNonNullablePropertiesSchemaFilter` qo'shilgach) endi to'g'ri chiqadi — haqiqatan
 * majburiy maydonlar (masalan `GetSchoolInfoResult.name`/`tests`, `StartSessionResult.
 * sessionToken`/`assessmentId`/`tests`, `PublicTestSummaryDto.code`/`order`) TypeScript'da ham
 * majburiy, shu sabab ularni ishlatuvchi kodda (`LandingPage`, `RegistrationPage`,
 * `lib/nextTest.ts`) endi qo'shimcha `?? ''`/`?? []` himoyasi YO'Q. Haqiqatan ixtiyoriy va
 * `nullable` maydonlar uchun (`accessCode`, `classLetter`, `parentPhone`, `email`,
 * `languageCode`, `currentTestCode`, `scaleLabels`, `options`, `currentValue`) null-ishlash
 * ataylab qoldirilgan — ular chindan yo'q bo'lishi mumkin.
 */

export type Gender = NonNullable<components['schemas']['Gender']>;

/** `docs/07` 1.1-bo'lim — boshlanish ekranidagi bitta test blokining ta'rifi. */
export type PublicTestCatalogItem = components['schemas']['PublicTestCatalogItemDto'];

/** `GET /api/public/schools/{slug}?k=` — `docs/07` 1.1-bo'lim javobi. */
export type PublicSchoolInfo = components['schemas']['GetSchoolInfoResult'];

/** `POST /api/public/sessions` so'rov tanasi — `docs/07` 1.2-bo'lim. */
export type StartSessionRequestBody = components['schemas']['StartSessionRequest'];

/** Sessiya ichidagi bitta test blokining ommaviy holati — `docs/07` 1.2/1.3-bo'lim. */
export type PublicTestSummary = components['schemas']['PublicTestSummaryDto'];

/** `POST /api/public/sessions` javobi (`201`/`resumed: true` bo'lsa `200`) — `docs/07` 1.2-bo'lim. */
export type StartSessionResponse = components['schemas']['StartSessionResult'];

/** `GET /api/public/sessions/me` javobi — `docs/07` 1.3-bo'lim. */
export type SessionStateResponse = components['schemas']['GetSessionStateResult'];
