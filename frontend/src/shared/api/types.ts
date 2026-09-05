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
  /**
   * RFC 9457 "extension members" — backend `ProblemDetails.Extensions` ga qo'shgan har qanday
   * qo'shimcha maydon (masalan `POST /catalog/tests/{id}/publish` ning `issues[]`,
   * `docs/07` 3.4-bo'lim). Shakli endpointdan endpointga farq qiladi, shu sabab `unknown` —
   * `AppError.extensions` orqali yetib boradi va ishlatuvchi feature uni o'zi tekshiradi.
   */
  [key: string]: unknown;
}

/**
 * `PagedResult<T>` — admin ro'yxat endpointlari uchun umumiy shakl.
 * docs/07-api-shartnoma.md, 4-bo'lim ("Umumiy konvensiyalar — Pagination") shakli bilan aynan
 * bir xil qilib to'g'irlandi (P23, `features/schools`): oldingi qisqartirilgan shakl (`total`
 * yolg'iz) hech qayerda ishlatilmagan edi, endi bu tipni birinchi ishlatuvchi shu — kelasi
 * ro'yxatlar (P24 o'quvchilar, P29) ham shu haqiqiy shaklga tayanadi.
 */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
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

/** `docs/07` 1.1-bo'lim — boshlanish ekranidagi bitta test blokining ta'rifi (ORQAGA MOSLIK — barcha nashr qilingan testlar, dasturdan qat'i nazar). */
export type PublicTestCatalogItem = components['schemas']['PublicTestCatalogItemDto'];

/** `GET /api/public/schools/{slug}?k=` — `docs/07` 1.1-bo'lim javobi. */
export type PublicSchoolInfo = components['schemas']['GetSchoolInfoResult'];

/**
 * Maktab uchun mavjud bitta dastur (`docs/06` 8-bo'lim, `prompts/34`/`prompts/36`) —
 * `GetSchoolInfoResult.programs[]`. Bir nechta bo'lsa Landing (E-1) shulardan tanlov
 * ko'rsatadi; `code` — `POST /sessions` so'rov tanasidagi `programCode`ga uzatiladi.
 */
export type PublicProgramSummary = components['schemas']['PublicProgramSummaryDto'];

/** `POST /api/public/sessions` so'rov tanasi — `docs/07` 1.2-bo'lim. */
export type StartSessionRequestBody = components['schemas']['StartSessionRequest'];

/** Sessiya ichidagi bitta test blokining ommaviy holati — `docs/07` 1.2/1.3-bo'lim. */
export type PublicTestSummary = components['schemas']['PublicTestSummaryDto'];

/** `POST /api/public/sessions` javobi (`201`/`resumed: true` bo'lsa `200`) — `docs/07` 1.2-bo'lim. */
export type StartSessionResponse = components['schemas']['StartSessionResult'];

/** `GET /api/public/sessions/me` javobi — `docs/07` 1.3-bo'lim. */
export type SessionStateResponse = components['schemas']['GetSessionStateResult'];

/**
 * Test oqimi (E-3) DTO'lari — docs/07-api-shartnoma.md, 1.4–1.6-bo'lim (`prompts/21`).
 * `PublicSessionController`ning shu 3 endpoint'i P11'da tayyor bo'lgani uchun `schema.d.ts`da
 * to'liq generatsiya qilingan — quyidagilar ham re-export, qo'lda yozilgan nusxa YO'Q.
 */

/** `POST /sessions/tests/{testCode}/start` javobi — `docs/07` 1.4-bo'lim. */
export type StartTestResponse = components['schemas']['StartTestResult'];

/** `GET /sessions/tests/{testCode}/questions` javobi — `docs/07` 1.5-bo'lim. */
export type TestQuestionsResponse = components['schemas']['GetTestQuestionsResult'];

/** Bitta savol — `docs/07` 1.5-bo'lim. `scale`/`scaleDirection` backend tomonidan hech qachon yuborilmaydi. */
export type PublicQuestion = components['schemas']['PublicQuestionDto'];

/** Ko'p tanlovli savol varianti (`type !== 'Likert5'` bo'lganda) — `docs/07` 1.5-bo'lim. */
export type PublicAnswerOption = components['schemas']['PublicAnswerOptionDto'];

/** Likert shkalasi yorlig'i (1..5) — `docs/07` 1.5-bo'lim. */
export type PublicScaleLabel = components['schemas']['PublicScaleLabelDto'];

/** `POST /sessions/tests/{testCode}/answers` so'rov elementi — `docs/07` 1.6-bo'lim. */
export type SaveAnswerItem = components['schemas']['SaveAnswerItemRequest'];

/** `POST /sessions/tests/{testCode}/answers` javobi — `docs/07` 1.6-bo'lim. */
export type SaveAnswersResponse = components['schemas']['SaveAnswersResult'];

/**
 * Test/sessiya yakunlash va o'quvchi natijasi — docs/07-api-shartnoma.md, 1.7–1.9-bo'lim.
 *
 * P12 (`CompleteTest`/`CompleteSession`/`GetStudentResult`) backendda tayyor va sxemada bor
 * (`CompleteTestResult`, `CompleteSessionResult`, `GetStudentResultResult`) — quyidagilar
 * ham re-export, qo'lda yozilgan nusxa YO'Q.
 */

/** `POST /sessions/tests/{testCode}/complete` javobi — `docs/07` 1.7-bo'lim. */
export type CompleteTestResponse = components['schemas']['CompleteTestResult'];

/** `POST /sessions/complete` javobi — `docs/07` 1.8-bo'lim. */
export type CompleteSessionResponse = components['schemas']['CompleteSessionResult'];

/** `GET /sessions/result` javobi — `docs/07` 1.9-bo'lim. Aktivlik ball/bayroq/xom ball YO'Q. */
export type StudentResultResponse = components['schemas']['GetStudentResultResult'];

/**
 * Ommaviy tanishtiruv (marketing) kontenti — docs/07-api-shartnoma.md, 1.10-bo'lim.
 * Sessiyaga bog'liq emas: `/metodika` sahifasidagi 16 tip bo'limi va `/metodika/:kod`
 * sahifalari shu javobdan quriladi. Bu yerda ham qo'lda yozilgan DTO YO'Q — `schema.d.ts` dan
 * re-export.
 */

/** `GET /api/public/type-catalog` javobi — `docs/07` 1.10-bo'lim. */
export type PublicTypeCatalog = components['schemas']['GetTypeCatalogResult'];

/** Bitta shaxsiyat tipining ochiq tavsifi (`type_catalog` yozuvi) — `docs/07` 1.10-bo'lim. */
export type PublicTypeCatalogItem = components['schemas']['PublicTypeCatalogItemDto'];

/**
 * Ommaviy foydalanuvchi kabineti (P47) — docs/07-api-shartnoma.md §2a va §5.
 * Bu yerda ham qo'lda yozilgan DTO YO'Q — `schema.d.ts` dan re-export.
 */

/**
 * `POST /api/auth/telegram` so'rov tanasi — Telegram Login Widget bergan obyekt
 * **o'zgartirilmasdan** (kalitlar `snake_case`, chunki imzo aynan shu nomlardan
 * hisoblangan — `docs/07` §2a.1).
 */
export type TelegramLoginRequestBody = components['schemas']['TelegramLoginRequest'];

/** `POST /api/auth/telegram` javobi — `docs/07` §2a.1. `refreshToken` tanada HECH QACHON yo'q. */
export type TelegramLoginResponse = components['schemas']['TelegramLoginResult'];

/** Ommaviy foydalanuvchi profili (`GET /api/me`) — `docs/07` §5.1. `telegramId` qaytarilmaydi. */
export type PublicUser = components['schemas']['PublicUserDto'];

/** `GET /api/me/assessments` javobi — `docs/07` §5.2. Sahifalash yo'q. */
export type MyAssessmentsResponse = components['schemas']['ListMyAssessmentsResult'];

/** Kabinetdagi bitta test sessiyasi — `docs/07` §5.2. Ball/indeks/bayroq maydonlari YO'Q. */
export type MyAssessment = components['schemas']['MyAssessmentDto'];

/** `POST /api/me/sessions` so'rov tanasi — `docs/07` §5.4 (maktabsiz, `slug`/`accessToken`siz). */
export type StartPublicSessionRequestBody = components['schemas']['StartPublicSessionRequest'];
