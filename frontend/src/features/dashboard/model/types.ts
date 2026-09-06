/**
 * Dashboard statistikasi DTO'lari — docs/07-api-shartnoma.md, 3.6-bo'lim (`GET
 * /api/admin/dashboard/stats?from=&to=`).
 *
 * Backend tayyor bo'lgach (`funnel`/`schoolBreakdown` qo'shilgach, PM tasdig'i
 * 2026-09-02: "643 test yashil, build 0 ogohlantirish") `npm run generate:api` ishga
 * tushirildi — quyidagilar endi **qo'lda yozilgan tiplar emas**, `schema.d.ts`dan
 * re-export (`shared/api/types.ts`dagi `PublicSchoolInfo` va h.k. bilan bir xil naqsh;
 * `docs/10`, 6-bo'lim: "Qo'lda yozilgan DTO tiplariga ruxsat yo'q — faqat generatsiya
 * yoki `types.ts`da re-export").
 *
 * `funnel`/`schoolBreakdown` endi **majburiy** maydonlar (avval ixtiyoriy edi, backend
 * hali yozilayotgani uchun) — `AdminDashboardStatsDto`da `required` ro'yxatida.
 *
 * `null` qoidasi (CLAUDE.md "MAXSUS DIQQAT" / `docs/06` §8, 2026-09-02, PM tasdig'i):
 * `last30Days.avgDurationMinutes`/`avgReliability`/`dropOffRate` va
 * `schoolBreakdown[].completionRate` — ma'lumot yo'qligida `null` (`0` EMAS). Backend
 * bu maydonlarni Swashbuckle nullable-value-type sxemasi bo'yicha optsional+`| null`
 * qilib chiqaradi (`avgReliability?: number | null`) — amalda backend ularni hech qachon
 * tashlab ketmaydi (har doim `null` yoki raqam), shu sabab ishlatuvchi kodda `!= null`
 * (loose, `undefined`ni ham qamrab oladi) tekshiruvi ishlatiladi, `?? 0` EMAS.
 */
import type { components } from '@/shared/api/schema';
import type { DashboardSource } from './dashboardSource';

/** `docs/07` 3.6-bo'lim — `totals` bloki. */
export type DashboardTotals = components['schemas']['AdminDashboardTotalsDto'];

/** `docs/07` 3.6-bo'lim — `last30Days` bloki. */
export type DashboardLast30Days = components['schemas']['AdminDashboardLast30DaysDto'];

/** `docs/07` 3.6-bo'lim: `personalityDistribution[]` — 16 tipli model kodi bo'yicha son. */
export type PersonalityDistributionItem = components['schemas']['AdminDashboardPersonalityItemDto'];

/** `docs/07` 3.6-bo'lim: `activityDistribution[]`. */
export type ActivityDistributionItem = components['schemas']['AdminDashboardActivityItemDto'];

/** `docs/07` 3.6-bo'lim: `hollandTop[]` — eng ko'p uchragan Holland kodlari. */
export type HollandTopItem = components['schemas']['AdminDashboardHollandItemDto'];

/** `docs/07` 3.6-bo'lim: `recentAssessments[]`. */
export type RecentAssessmentItem = components['schemas']['AdminDashboardRecentAssessmentDto'];

/**
 * Voronka bosqichlari — `linkViews → registered → started → completed → analyzed`
 * (backend `[from,to]` oynasiga bog'liq hisoblaydi, PM tavsifi 2026-09-02). Tartib
 * `FUNNEL_STEP_ORDER` (`funnel.ts`) bilan bir xil.
 */
export type DashboardFunnel = components['schemas']['AdminDashboardFunnelDto'];

/**
 * `schoolBreakdown[]` qatori — maksimum 20 qator, `registered` DESC → `completed`
 * DESC → `linkViews` DESC tartiblangan (backend, PM tavsifi 2026-09-02).
 */
export type SchoolBreakdownItem = components['schemas']['AdminDashboardSchoolBreakdownItemDto'];

/** `GET /api/admin/dashboard/stats` — 200 javobi (`docs/07`, 3.6-bo'lim). */
export type DashboardStatsResponse = components['schemas']['AdminDashboardStatsDto'];

/**
 * `GET /api/admin/dashboard/stats?from=&to=` so'rov parametrlari — ikkalasi ham
 * ixtiyoriy. So'rov parametri (backend DTO emas) — `SchoolsListQuery`/`StudentsListQuery`
 * bilan bir xil sabab bilan qo'lda yozilgan (docs/10 §6dagi qoida javob tanasi/domain
 * DTO'lariga tegishli, ro'yxat/filtr so'rov shakllariga emas).
 */
export interface DashboardStatsQuery {
  from?: string;
  to?: string;
  /**
   * Manba kesimi (P48). Backend standarti ham `school`, lekin bu yerda ATAYLAB majburiy
   * emas: chaqiruvchi (`DashboardPage`) uni har doim ANIQ uzatadi, shunda kesh kaliti ham
   * ("qaysi kesim ko'rilgan") aniq bo'ladi.
   */
  source?: DashboardSource;
}
