/**
 * Testlar uchun **tiplangan** API mock yordamchilari.
 *
 * ## Nega kerak
 *
 * Ilgari har test faylida o'zining `function jsonResponse(body: unknown, …)` nusxasi bor edi.
 * `unknown` hech narsani tekshirmaydi — mock backend shartnomasidan uzilib qolsa ham test
 * YASHIL qolardi, chunki test frontendning o'z taxminini tasdiqlardi, backendni emas.
 * Aynan shu naqsh 2026-09-02/03 da oltita nomuvofiqlikka olib keldi (`results.mbti16` ↔
 * `MBTI16`, RIASEC `A` ↔ `ART`, `backupCodes` ↔ `recoveryCodes`, `currentPassword` ↔
 * `password`, AI provayder `baseUrl`, AI hisobotining 5 bo'limi).
 *
 * Endi mock tanasi sxemadan olingan tip bilan tekshiriladi: mock backenddan uzilsa
 * `npm run typecheck` **qizaradi**, test jimgina yashil qolmaydi.
 *
 * ## Qaysi biri qachon
 *
 * | Yordamchi | Qachon |
 * |---|---|
 * | `jsonResponse<'SchemaNomi'>(body)` | Javob `schema.d.ts` da BOR — DEFAULT tanlov |
 * | `listResponse<'SchemaNomi'>(items)` | Sahifalanmagan massiv javob (`[...]`) |
 * | `pagedResponse<'SchemaNomi'>(items)` | Sahifalangan ro'yxat (`docs/07` §4) |
 * | `typedResponse<FeatureDto>(body)` | Sxemada HALI yo'q yoki sxemasi ESKIRGAN javob |
 * | `problemResponse(code, status)` | `ProblemDetails` xato javobi (`docs/06` §6) |
 * | `emptyResponse(status)` | Tanasiz javob (`204`, `POST … → 200` bo'sh) |
 *
 * `typedResponse` — ATAYLAB "ochiq" eshik, lekin tip argumenti AYNAN ko'rsatilishi shart
 * (`typedResponse<CatalogTestListItem[]>([...])`), aks holda u ham hech narsa tekshirmaydi.
 * Sxema yangilangach (`npm run generate:api`) shunday chaqiruvlar `jsonResponse<'…'>` ga
 * o'tkaziladi.
 */
import type { components } from '@/shared/api/schema';
import type { PagedResult, ProblemDetails } from '@/shared/api/types';

/** `schema.d.ts` dagi barcha backend DTO'lari. */
export type Schemas = components['schemas'];

/** Sxemadagi DTO nomi — `jsonResponse<'AdminSchoolDetailDto'>(…)` shaklida ishlatiladi. */
export type SchemaName = keyof Schemas;

const JSON_HEADERS = { 'content-type': 'application/json' } as const;

function buildResponse(body: unknown, status: number): Response {
  return new Response(JSON.stringify(body), { status, headers: { ...JSON_HEADERS } });
}

/**
 * Sxemadan tiplangan javob. Sxema nomi AYNAN ko'rsatiladi:
 *
 * ```ts
 * fetchMock.mockResolvedValue(jsonResponse<'LoginResult'>(LOGIN_RESULT));
 * ```
 *
 * Tana sxemaga mos kelmasa (maydon yo'q, ortiqcha maydon, tip boshqa) `tsc` xato beradi.
 */
export function jsonResponse<K extends SchemaName>(body: Schemas[K], status = 200): Response {
  return buildResponse(body, status);
}

/**
 * Sahifalanmagan MASSIV javob — sxemadan tiplangan element bilan:
 *
 * ```ts
 * listResponse<'AdminAiProviderDto'>([GEMINI_CONFIG, OPENAI_CONFIG]);
 * ```
 *
 * `jsonResponse<'X'[]>` ishlamaydi: `jsonResponse` tip argumenti sxema NOMI (`keyof
 * Schemas`), tipning o'zi emas — shu sabab massiv javoblar uchun alohida yordamchi.
 */
export function listResponse<K extends SchemaName>(items: Schemas[K][], status = 200): Response {
  return buildResponse(items, status);
}

/**
 * Sxemada hali mavjud bo'lmagan (yoki sxemasi eskirgan) javob uchun. Tip argumenti
 * **majburiy** — `typedResponse<ImportValidationResult>({...})`; aks holda tekshiruv yo'q.
 */
export function typedResponse<T>(body: T, status = 200): Response {
  return buildResponse(body, status);
}

/**
 * Sahifalangan ro'yxat javobi — `docs/07-api-shartnoma.md` §4 ("Pagination") shakli.
 * Elementlar sxemadan tiplanadi:
 *
 * ```ts
 * pagedResponse<'AdminSchoolListItemDto'>([SCHOOL_1]);
 * ```
 */
export function pagedResponse<K extends SchemaName>(
  items: Schemas[K][],
  overrides: Partial<Omit<PagedResult<Schemas[K]>, 'items'>> = {},
  status = 200,
): Response {
  const page = overrides.page ?? 1;
  const pageSize = overrides.pageSize ?? 20;
  const totalCount = overrides.totalCount ?? items.length;
  const totalPages = overrides.totalPages ?? Math.max(1, Math.ceil(totalCount / pageSize));
  const body: PagedResult<Schemas[K]> = {
    items,
    page,
    pageSize,
    totalCount,
    totalPages,
    hasNext: overrides.hasNext ?? page < totalPages,
    hasPrevious: overrides.hasPrevious ?? page > 1,
  };
  return buildResponse(body, status);
}

/**
 * `pagedResponse` ning sxemasiz varianti — element tipi feature'ning qo'lda yozilgan
 * DTO'si bo'lganda (`features/catalog`, `features/ai-settings`). Tip AYNAN ko'rsatiladi.
 */
export function typedPagedResponse<T>(
  items: T[],
  overrides: Partial<Omit<PagedResult<T>, 'items'>> = {},
  status = 200,
): Response {
  const page = overrides.page ?? 1;
  const pageSize = overrides.pageSize ?? 20;
  const totalCount = overrides.totalCount ?? items.length;
  const totalPages = overrides.totalPages ?? Math.max(1, Math.ceil(totalCount / pageSize));
  const body: PagedResult<T> = {
    items,
    page,
    pageSize,
    totalCount,
    totalPages,
    hasNext: overrides.hasNext ?? page < totalPages,
    hasPrevious: overrides.hasPrevious ?? page > 1,
  };
  return buildResponse(body, status);
}

/**
 * `application/problem+json` xato javobi — `docs/06-arxitektura.md` 6-bo'lim.
 * `extensions` — endpointga xos qo'shimcha maydonlar (masalan `issues[]`).
 */
export function problemResponse(
  code: string,
  status: number,
  detail?: string,
  extensions: Record<string, unknown> = {},
): Response {
  const body: ProblemDetails = {
    type: `https://studentroadmap/errors/${code}`,
    title: 'Xato',
    status,
    code,
    ...(detail === undefined ? {} : { detail }),
    ...extensions,
  };
  return buildResponse(body, status);
}

/** Tanasiz javob — `204 No Content` yoki bo'sh `200`. */
export function emptyResponse(status = 204): Response {
  return new Response(null, { status });
}
