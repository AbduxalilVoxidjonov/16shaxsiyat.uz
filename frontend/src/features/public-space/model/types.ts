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
 * Biriktirilgan, lekin O'CHIRILGAN dasturlar — ogohlantirishda aynan qaysi dastur
 * to'sqinlik qilayotganini ko'rsatish uchun (havola bilan). Egasining 2026-09-05 holati:
 * yagona `PERSONALITY_PROFILE` dasturi `Published`, lekin `isActive = false`.
 */
export function findInactivePrograms(
  programs: readonly PublicSpaceProgramDto[],
): PublicSpaceProgramDto[] {
  return programs.filter((program) => !program.isActive);
}
