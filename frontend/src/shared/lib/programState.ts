/**
 * Dasturning YAGONA holati (`ProgramState`) — nomlar, belgi rangi va i18n kaliti.
 *
 * Holat BACKENDDA hisoblanadi (`ProgramStateRules.Resolve`,
 * `src/StudentRoadMap.Domain/Catalog/ProgramState.cs`) va admin API'da bitta `state` maydoni
 * bo'lib keladi — frontend uni QAYTA HISOBLAMAYDI. Bu fayl faqat "qaysi qiymat qanday
 * ko'rinadi" savoliga javob beradi.
 *
 * **Nega `shared/lib`:** holatni ikki feature ko'rsatadi — `features/programs` (ro'yxat va
 * detal) hamda `features/public-space` (biriktirilgan dasturlar) — feature'lar esa
 * bir-birini import qila olmaydi (`docs/10` 2-bo'lim). Ikki joyda takrorlansa, ular
 * muqarrar ravishda ajralib ketadi: 2026-09-06 gacha aynan shunday bo'lgan edi va bitta
 * dastur bir joyda "Arxiv", boshqasida "Faol" bo'lib ko'rinardi.
 */
import type { BadgeVariant } from '@/shared/ui/Badge';

/**
 * Backend `ProgramState` enum qiymatlari. Sxemada oddiy `string` sifatida chiqadi
 * (`ToString()`), shu sabab ro'yxat qo'lda sinxronlanadi — `features/students/model/enums.ts`
 * dagi naqsh.
 */
export const PROGRAM_STATE_VALUES = ['Draft', 'Active', 'Paused', 'Archived'] as const;

export type ProgramState = (typeof PROGRAM_STATE_VALUES)[number];

export function isProgramState(value: string | null | undefined): value is ProgramState {
  return value != null && (PROGRAM_STATE_VALUES as readonly string[]).includes(value);
}

const BADGE_VARIANT: Record<ProgramState, BadgeVariant> = {
  Draft: 'neutral',
  Active: 'success',
  Paused: 'warning',
  Archived: 'danger',
};

export function programStateBadgeVariant(state: string): BadgeVariant {
  return isProgramState(state) ? BADGE_VARIANT[state] : 'neutral';
}

/**
 * i18n kaliti. Noma'lum qiymat kelsa `programs.state.unknown` — jimgina BO'SH belgi EMAS
 * ("ma'lumot yo'q ≠ nol", `docs/06` qarorlar jurnali).
 */
export function programStateLabelKey(state: string): string {
  return isProgramState(state) ? `programs.state.${state.toLowerCase()}` : 'programs.state.unknown';
}
