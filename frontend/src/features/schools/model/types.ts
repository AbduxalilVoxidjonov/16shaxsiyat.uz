/**
 * Maktablar admin DTO'lari — `docs/07-api-shartnoma.md` 3.1-bo'lim.
 *
 * Backend (P14, `SchoolsController`) tayyor va sxemada bor, shu sabab BARCHA tip
 * `shared/api/schema.d.ts` dan **re-export** (`docs/10` 6-bo'lim qoidasi:
 * "Qo'lda yozilgan DTO tiplariga ruxsat yo'q"). Qo'lda yozilgan DTO bu faylda YO'Q.
 */
import type { components } from '@/shared/api/schema';

/** `GET /api/admin/schools` ro'yxat qatori — backend `AdminSchoolListItemDto`. */
export type SchoolListItemDto = components['schemas']['AdminSchoolListItemDto'];

/**
 * Maktabning ISHTIROK statistikasi — `SchoolDetailDto.stats` (`docs/07` 3.1),
 * backend `AdminSchoolStatsDto` dan re-export.
 *
 * **Birliklar (`docs/07` 3.1 va 3.6 bilan bir xil qoida):**
 * - `completionRate` — **ulush (0..1), foiz EMAS**: ko'rsatishdan oldin `× 100` qilinadi
 *   (`dashboard` dagi `SchoolBreakdownTable` bilan bir xil; ilgari aynan shu birlik
 *   chalkashligi sababli 50% yakunlagan maktab "1%" ko'ringan). Hech kim ro'yxatdan
 *   o'tmagan bo'lsa — `null` (nisbat ANIQLANMAGAN), `0` EMAS.
 * - `lastActivityAt` — ma'lumot yo'q bo'lsa `null` (UI'da `—`).
 * - Uchta hisoblagich esa DOIM son: hech kim topshirmagan bo'lsa `0` ko'rsatiladi, `—` emas.
 */
export type SchoolStatsDto = components['schemas']['AdminSchoolStatsDto'];

/**
 * `GET /api/admin/schools/{id}` — "Batafsil + statistika" (`docs/07` 3.1).
 *
 * To'liq re-export: `stats` bloki ham sxemadan keladi — shakl o'zgarsa `tsc` qizaradi.
 */
export type SchoolDetailDto = components['schemas']['AdminSchoolDetailDto'];

/**
 * Maktab havolasi ISHLAYDIMI — `docs/07` 3.1 (2026-09-03). Ro'yxatda ham, detalda ham bor.
 *
 * `status` — backend `SchoolLinkHealthStatus` nomi (string): `Ok` | `NoProgramsAtAll` |
 * `NoProgramAssigned` | `ProgramsDeactivated` | `ProgramsWithoutTests`. Sxemada oddiy `string`
 * (backend enum'ni `ToString()` bilan qaytaradi) — shu sabab union `SCHOOL_LINK_HEALTH_STATUSES`
 * da qo'lda saqlanadi (`programs/model/types.ts` dagi naqsh bilan bir xil).
 *
 * `availableProgramCount === 0` ⟺ o'quvchi havolani ochsa `409 NO_PROGRAM_AVAILABLE` oladi.
 */
export type SchoolLinkHealthDto = components['schemas']['AdminSchoolLinkHealthDto'];

/** `GET /api/admin/schools/link-health` javobi — dashboard banneri uchun. */
export type SchoolsLinkHealthDto = components['schemas']['AdminSchoolsLinkHealthDto'];

/** Havolasi ishlamaydigan bitta maktab (sabab bilan). */
export type BrokenSchoolLinkDto = components['schemas']['AdminBrokenSchoolLinkDto'];

/**
 * Sabab matni uchun kalitlar. Backenddagi `SchoolLinkHealthStatus` bilan qo'lda sinxron —
 * yangi qiymat kelsa `linkHealthReasonKey` uni `unknown` ga tushiradi (jimgina bo'sh matn
 * EMAS: "ma'lumot yo'q ≠ nol", `docs/06` qarorlar jurnali).
 */
export const SCHOOL_LINK_HEALTH_STATUSES = [
  'Ok',
  'NoProgramsAtAll',
  'NoProgramAssigned',
  'ProgramsDeactivated',
  'ProgramsWithoutTests',
] as const;

export type SchoolLinkHealthStatus = (typeof SCHOOL_LINK_HEALTH_STATUSES)[number];

/** Havola BUTUNLAY ishlamaydimi (o'quvchi testga umuman kira olmaydi). */
export function isSchoolLinkBroken(linkHealth: SchoolLinkHealthDto | undefined): boolean {
  return linkHealth !== undefined && linkHealth.status !== 'Ok';
}

/** i18n kaliti — noma'lum status uchun `unknown` (jimgina bo'sh matn qaytarilmaydi). */
export function linkHealthReasonKey(status: string): string {
  return (SCHOOL_LINK_HEALTH_STATUSES as readonly string[]).includes(status)
    ? `schools.linkHealth.reason.${status}`
    : 'schools.linkHealth.reason.unknown';
}

/** `GET /api/admin/schools` so'rov parametrlari — `docs/07` 3.1 (DTO emas, query shakli). */
export interface SchoolsListQuery {
  search?: string;
  region?: string;
  isActive?: boolean;
  page: number;
  pageSize: number;
  sort?: string;
}

/**
 * `POST /api/admin/schools` / `PUT /api/admin/schools/{id}` so'rov tanasi — `docs/02` FR-1.1.
 *
 * Backendda ikki alohida sxema bor (`CreateSchoolRequest`, `UpdateSchoolRequest`), lekin
 * ular HARFMA-HARF bir xil, shu sabab UI bitta forma tipini ishlatadi. Agar backend ularni
 * ajratsa — `tsc` shu yerda qizaradi va forma bo'linadi.
 */
export type SchoolUpsertRequest = components['schemas']['CreateSchoolRequest'] &
  components['schemas']['UpdateSchoolRequest'];

/** `POST /api/admin/schools/{id}/regenerate-link` javobi — `docs/07` 3.1. */
export type RegenerateLinkResponse = components['schemas']['RegenerateSchoolLinkResult'];

/**
 * Maktab o'chirishda `409` (o'quvchisi bor) holatida kutilgan `ProblemDetails.code`.
 *
 * TODO: `docs/06` 6-bo'lim xato kodlari jadvalida maktab o'chirish uchun alohida kod yo'q,
 * faqat "o'quvchisi bo'lsa 409" deyilgan (`docs/07` 3.1) — kod nomi taxmin qilingan.
 */
export const SCHOOL_ERROR_CODES = {
  hasStudents: 'SCHOOL_HAS_STUDENTS',
} as const;
