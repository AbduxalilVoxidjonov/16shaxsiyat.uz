/**
 * Ommaviy makon admin DTO'lari (P48) — `GET/POST/DELETE/PUT /api/admin/public-space`.
 *
 * Backend tayyor va sxemada bor, shu sabab BARCHA tip `shared/api/schema.d.ts` dan
 * **re-export** (`docs/10` 6-bo'lim qoidasi: "Qo'lda yozilgan DTO tiplariga ruxsat yo'q").
 *
 * ## Atamalar
 *
 * Bu bo'limda "maktab" so'zi ISHLATILMAYDI — ommaviy makon maktab emas, u Telegram orqali
 * kirgan tashqi foydalanuvchilar makoni. Shu sabab UI matnlari "makon", "foydalanuvchilar",
 * "sessiyalar", "havola" atamalari bilan quriladi. Backendda esa yozuv texnik jihatdan
 * `School` jadvalida turadi (`SchoolKind.PublicSpace`) — bu SAQLASH qarori, atama emas.
 */
import type { components } from '@/shared/api/schema';
import type { BadgeVariant } from '@/shared/ui/Badge';

/** `GET /api/admin/public-space` javobi — backend `AdminPublicSpaceDto`. */
export type PublicSpaceDto = components['schemas']['AdminPublicSpaceDto'];

/**
 * Makonda test boshlanadimi — backend `AdminPublicSpaceAvailabilityDto`.
 *
 * Mezon ommaviy oqim handleri bilan AYNAN bir xil manbadan hisoblanadi (backend), frontend
 * uni qayta ixtiro QILMAYDI: aks holda panel "hammasi joyida" deb yolg'on aytardi
 * (2026-09-03 jonli hodisasi).
 */
export type PublicSpaceAvailabilityDto =
  components['schemas']['AdminPublicSpaceAvailabilityDto'];

/** Biriktirilgan bitta dastur — backend `AdminPublicSpaceProgramDto`. */
export type PublicSpaceProgramDto = components['schemas']['AdminPublicSpaceProgramDto'];

/** Ishtirok statistikasi — backend `AdminPublicSpaceStatsDto`. */
export type PublicSpaceStatsDto = components['schemas']['AdminPublicSpaceStatsDto'];

/** `PUT /api/admin/public-space/show-result` so'rov tanasi. */
export type SetShowResultRequestBody =
  components['schemas']['SetPublicSpaceShowResultRequest'];

/**
 * Sabab matni uchun kalitlar. Backenddagi `SchoolLinkHealthStatus` qiymatlari bilan qo'lda
 * sinxron (sxemada oddiy `string` — backend enum'ni `ToString()` bilan qaytaradi). Noma'lum
 * qiymat kelsa `unknown` ga tushadi — jimgina BO'SH matn EMAS ("ma'lumot yo'q ≠ nol",
 * `docs/06` qarorlar jurnali).
 */
export const PUBLIC_SPACE_AVAILABILITY_STATUSES = [
  'Ok',
  'NoProgramsAtAll',
  'NoProgramAssigned',
  'ProgramsDeactivated',
  'ProgramsWithoutTests',
] as const;

export type PublicSpaceAvailabilityStatus =
  (typeof PUBLIC_SPACE_AVAILABILITY_STATUSES)[number];

/** Hozir kimdir test boshlay oladimi (`Ok` dan boshqa har qanday holat — yo'q). */
export function isPublicSpaceBlocked(availability: PublicSpaceAvailabilityDto): boolean {
  return availability.status !== 'Ok';
}

/** i18n kaliti — noma'lum status uchun `unknown`. */
export function availabilityReasonKey(status: string): string {
  return (PUBLIC_SPACE_AVAILABILITY_STATUSES as readonly string[]).includes(status)
    ? `publicSpace.availability.reason.${status}`
    : 'publicSpace.availability.reason.unknown';
}

/**
 * Biriktirilgan, lekin ISHLAMAYDIGAN dasturlar — ogohlantirishda aynan qaysi dastur
 * to'sqinlik qilayotganini ko'rsatish uchun (havola bilan).
 *
 * **2026-09-06:** mezon endi YAGONA holatga tayanadi — "ishlaydigan" holat faqat bitta:
 * `Active`. Ilgari bu yerda `!isActive` tekshirilardi va u `Draft`/`Archived` dasturni
 * "joyida" deb hisoblab yuborardi, garchi ular ham test boshlashga yaramasa ham.
 */
export function findBlockedPrograms(
  programs: readonly PublicSpaceProgramDto[],
): PublicSpaceProgramDto[] {
  return programs.filter((program) => program.state !== 'Active');
}

// ————— Foydalanuvchilar ro'yxati (`GET /api/admin/public-space/users`, 2026-09-07) —————

/** Ro'yxat qatori — backend `AdminPublicUserListItemDto`. */
export type PublicSpaceUserDto = components['schemas']['AdminPublicUserListItemDto'];

/** Telegram profili — backend `AdminPublicUserTelegramDto`. */
export type PublicSpaceUserTelegramDto = components['schemas']['AdminPublicUserTelegramDto'];

/** Sessiyalar soni — backend `AdminPublicUserAssessmentCountsDto`. */
export type PublicSpaceUserAssessmentCountsDto =
  components['schemas']['AdminPublicUserAssessmentCountsDto'];

/** Oxirgi sessiya — backend `AdminPublicUserLastAssessmentDto`. */
export type PublicSpaceUserLastAssessmentDto =
  components['schemas']['AdminPublicUserLastAssessmentDto'];

/**
 * "Qayerda to'xtagan" — backend `AdminPublicUserProgressDto`. Hisob ommaviy
 * `GET /api/public/sessions/me` bilan BIR XIL qoidadan (`SessionProgressCalculator`):
 * `DisplayOrder` bo'yicha birinchi yakunlanmagan blok joriy, `answered/questionsTotal`
 * — shu blokdagi javoblar. Frontend buni qayta HISOBLAMAYDI, faqat ko'rsatadi.
 */
export type PublicSpaceUserProgressDto = components['schemas']['AdminPublicUserProgressDto'];

/**
 * `?status=` filtri qiymatlari (backend `PublicUserStatusFilter`) — OXIRGI sessiya bo'yicha:
 * `never_started` — birorta sessiya yo'q (anketa to'ldirilmagan bo'lsa ham); `in_progress` —
 * oxirgi sessiya yakunlanmagan (`Draft`/`InProgress`/`Abandoned`); `completed` — yakunlangan;
 * `deleted` — akkaunt o'chirilgan (2026-09-08, egasining talabi: o'chirilgan akkauntlar
 * ro'yxatdan yo'qolmaydi, alohida filtrlash mumkin bo'ladi).
 */
export const PUBLIC_USER_STATUS_FILTERS = [
  'all',
  'never_started',
  'in_progress',
  'completed',
  'deleted',
] as const;
export type PublicUserStatusFilter = (typeof PUBLIC_USER_STATUS_FILTERS)[number];

/** URL'dan kelgan qiymat haqiqiy filtrmi (noma'lum → `all`). */
export function parsePublicUserStatusFilter(value: string | null): PublicUserStatusFilter {
  return value && (PUBLIC_USER_STATUS_FILTERS as readonly string[]).includes(value)
    ? (value as PublicUserStatusFilter)
    : 'all';
}

/** `GET /api/admin/public-space/users` so'rov parametrlari (DTO emas, query shakli). */
export interface PublicSpaceUsersQuery {
  search?: string;
  /** `all` yoki bo'sh — parametr yuborilmaydi. */
  status?: PublicUserStatusFilter;
  page: number;
  pageSize: number;
  sort?: string;
}

/**
 * Qatorning ko'rinadigan holati — `lastAssessment.status` (`AssessmentStatus` nomi, sxemada
 * oddiy `string`) dan hosila. `features/students` dagi jadval ATAYLAB import qilinmaydi
 * (`docs/10` 2-bo'lim: feature'lar bir-birini import qilmaydi) — bu yerda o'z guruhlashi:
 * admin uchun "Tugallangan" va "Tahlil qilingan" farqi muhim, qolgan tafsilot esa emas.
 */
export const PUBLIC_USER_DISPLAY_STATUSES = [
  'NotStarted',
  'InProgress',
  'Completed',
  'Analyzed',
  'AnalysisFailed',
  'Abandoned',
  'Deleted',
  'Unknown',
] as const;
export type PublicUserDisplayStatus = (typeof PUBLIC_USER_DISPLAY_STATUSES)[number];

/**
 * Qator berilgani — 2026-09-08, egasining talabi: o'chirilgan akkauntlar endi ro'yxatda
 * turaveradi (`deletedAt != null`), shu sabab holatni faqat oxirgi sessiyadan emas, butun
 * qatordan hisoblash kerak. O'chirilganlik OXIRGI sessiya holatidan QAT'I NAZAR ustunlik
 * qiladi — akkaunt tugallangan test bilan o'chirilgan bo'lsa ham ro'yxatda "O'chirilgan"
 * ko'rinadi, aks holda admin buni payqamay qolardi.
 */
export function toPublicUserDisplayStatus(row: PublicSpaceUserDto): PublicUserDisplayStatus {
  if (row.deletedAt != null) return 'Deleted';
  const lastAssessment = row.lastAssessment;
  if (!lastAssessment) return 'NotStarted';
  switch (lastAssessment.status) {
    case 'Draft':
    case 'InProgress':
      return 'InProgress';
    case 'Completed':
    case 'Analyzing':
      return 'Completed';
    case 'Analyzed':
      return 'Analyzed';
    case 'AnalysisFailed':
      return 'AnalysisFailed';
    case 'Abandoned':
      return 'Abandoned';
    default:
      // Noma'lum qiymat jimgina "Boshlamagan" bo'lib qolmasin — "ma'lumot yo'q ≠ nol".
      return 'Unknown';
  }
}

export const PUBLIC_USER_STATUS_BADGE_VARIANT: Record<PublicUserDisplayStatus, BadgeVariant> = {
  NotStarted: 'neutral',
  InProgress: 'primary',
  Completed: 'success',
  Analyzed: 'success',
  AnalysisFailed: 'danger',
  Abandoned: 'warning',
  Deleted: 'danger',
  Unknown: 'neutral',
};
