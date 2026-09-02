/**
 * Maktablar admin DTO'lari — docs/07-api-shartnoma.md, 3.1-bo'lim; docs/05-database-schema.md
 * `schools` jadvali (ustunlar: `name`, `region`, `district`, `school_number`, `contact_person`,
 * `contact_phone`, `slug`, `access_token`, `access_code`, `daily_registration_limit`,
 * `is_active`, `notes`).
 *
 * // TODO: P14 (maktablar/o'quvchilar admin API) tugagach `npm run generate:api` ishga
 * // tushiriladi va quyidagi qo'lda yozilgan tiplar `schema.d.ts`dan re-export bilan
 * // almashtiriladi (`docs/10`, 6-bo'lim qoidasi; xuddi `features/auth/model/types.ts` va
 * // `features/settings/model/types.ts`dagi TODO bilan bir xil naqsh). Ayniqsa quyidagilar
 * // backend tomon aniqlashguncha **taxminiy**:
 * // - `SchoolDetailDto.qrCodeBase64` — docs/07 3.1 faqat `POST .../regenerate-link` javobida
 * //   `{ publicUrl, qrCodeBase64 }` ekanini aytadi; `GET /api/admin/schools/{id}` javobida QR
 * //   borligi hujjatda aniq yozilmagan. QR modalni havolani qayta yaratmasdan ochish uchun
 * //   (`docs/11` A-3: "Havola ustuni... QR ikonkasi") detal javobida ham shu maydon bor deb
 * //   faraz qilingan — **PM/backend tasdiqlashi kerak** (hisobotga qarang).
 * // - `SchoolListDeleteConflict` uchun `code` qiymati (`SCHOOL_HAS_STUDENTS`) — docs/06,
 * //   6-bo'lim xato kodlari jadvalida maktab o'chirish uchun alohida kod yo'q, faqat
 * //   "o'quvchisi bo'lsa 409" deyilgan (docs/07, 3.1). Shu kod nomi taxmin qilingan.
 */

/** `SchoolListItemDto` — docs/07 3.1: ro'yxat qatori. */
export interface SchoolListItemDto {
  id: string;
  name: string;
  region: string;
  district: string;
  slug: string;
  publicUrl: string;
  isActive: boolean;
  studentCount: number;
  completedCount: number;
  lastActivityAt: string | null;
}

/**
 * Maktabning ISHTIROK statistikasi — `SchoolDetailDto.stats` (docs/07 3.1).
 *
 * **Birliklar (docs/07 3.1 va 3.6 bilan bir xil qoida):**
 * - `completionRate` — **ulush (0..1), foiz EMAS**: ko'rsatishdan oldin `× 100` qilinadi
 *   (`dashboard`dagi `SchoolBreakdownTable` bilan bir xil; ilgari aynan shu birlik chalkashligi
 *   sababli 50% yakunlagan maktab "1%" ko'ringan). Hech kim ro'yxatdan o'tmagan bo'lsa —
 *   `null` (nisbat ANIQLANMAGAN), `0` EMAS.
 * - `lastActivityAt` — ma'lumot yo'q bo'lsa `null` (UI'da `—`).
 * - Uchta hisoblagich esa DOIM son: hech kim topshirmagan bo'lsa `0` ko'rsatiladi, `—` emas.
 */
export interface SchoolStatsDto {
  /** Ro'yxatdan o'tgan o'quvchilar (o'chirilganlar kirmaydi). */
  studentCount: number;
  /** Testni to'liq yakunlagan o'quvchilar. */
  completedCount: number;
  /** Jarayondagi (boshlangan, lekin tugatilmagan) sessiyalar. */
  inProgressCount: number;
  /** Ulush 0..1 (foiz emas); `studentCount === 0` bo'lsa `null`. */
  completionRate: number | null;
  lastActivityAt: string | null;
}

/** `GET /api/admin/schools/{id}` — "Batafsil + statistika" (docs/07 3.1). */
export interface SchoolDetailDto {
  id: string;
  name: string;
  region: string;
  district: string;
  schoolNumber: string | null;
  contactPerson: string | null;
  contactPhone: string | null;
  dailyRegistrationLimit: number;
  accessCode: string | null;
  notes: string | null;
  slug: string;
  publicUrl: string;
  /** Taxminiy maydon — yuqoridagi TODO izohiga qarang. */
  qrCodeBase64: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  stats: SchoolStatsDto;
}

/** `GET /api/admin/schools` so'rov parametrlari — docs/07 3.1. */
export interface SchoolsListQuery {
  search?: string;
  region?: string;
  isActive?: boolean;
  page: number;
  pageSize: number;
  sort?: string;
}

/** `POST /api/admin/schools` / `PUT /api/admin/schools/{id}` so'rov tanasi — docs/02 FR-1.1. */
export interface SchoolUpsertRequest {
  name: string;
  region: string;
  district: string;
  schoolNumber?: string;
  contactPerson?: string;
  contactPhone?: string;
  dailyRegistrationLimit: number;
  accessCode?: string;
  notes?: string;
}

/** `POST /api/admin/schools/{id}/regenerate-link` javobi — docs/07 3.1. */
export interface RegenerateLinkResponse {
  publicUrl: string;
  qrCodeBase64: string;
}

/** Maktab o'chirishda `409` (o'quvchisi bor) holatida kutilgan `ProblemDetails.code`. */
export const SCHOOL_ERROR_CODES = {
  hasStudents: 'SCHOOL_HAS_STUDENTS',
} as const;
