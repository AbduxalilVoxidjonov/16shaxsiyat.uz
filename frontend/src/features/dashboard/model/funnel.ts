import type { DashboardFunnel } from './types';

/** Voronka bosqichlari — shartnomada yozilgan tartib bilan bir xil (docs/07, 3.6-bo'lim). */
export const FUNNEL_STEP_ORDER: Array<keyof DashboardFunnel> = [
  'linkViews',
  'registered',
  'started',
  'completed',
  'analyzed',
];

export interface FunnelStep {
  key: keyof DashboardFunnel;
  count: number;
  /**
   * Oldingi bosqichga nisbatan foiz. Birinchi bosqichda va oldingi bosqich `0` bo'lganda
   * `null` — vazifa ko'rsatmasi: "oldingi bosqich 0 bo'lganda nol bo'linish yo'q". UI bu
   * holatda foizni umuman ko'rsatmaydi (raqam bilan bo'lish o'rniga NaN/Infinity emas).
   */
  percentOfPrevious: number | null;
}

/**
 * `DashboardFunnel`dan bosqichlar ro'yxatini quradi, har biriga oldingi bosqichga
 * nisbatan foizni hisoblaydi. Nol bo'linishdan himoyalangan — vazifa ko'rsatmasi ("PM
 * savoli"gacha): egasining asosiy savoli aynan shu voronka, shu sabab bu funksiya alohida
 * (UI'dan mustaqil) va to'liq test qilingan (`funnel.test.ts`).
 */
export function buildFunnelSteps(funnel: DashboardFunnel): FunnelStep[] {
  return FUNNEL_STEP_ORDER.map((key, index) => {
    const count = funnel[key];
    if (index === 0) {
      return { key, count, percentOfPrevious: null };
    }
    const previousKey = FUNNEL_STEP_ORDER[index - 1]!;
    const previousCount = funnel[previousKey];
    const percentOfPrevious = previousCount > 0 ? (count / previousCount) * 100 : null;
    return { key, count, percentOfPrevious };
  });
}
