import { describe, expect, it } from 'vitest';
import { buildFunnelSteps } from './funnel';

describe('buildFunnelSteps', () => {
  it("har bosqich uchun to'g'ri sonni qaytaradi va birinchi bosqichda foiz hisoblamaydi", () => {
    const steps = buildFunnelSteps({
      linkViews: 1000,
      registered: 400,
      started: 380,
      completed: 300,
      analyzed: 290,
    });

    expect(steps).toEqual([
      { key: 'linkViews', count: 1000, percentOfPrevious: null },
      { key: 'registered', count: 400, percentOfPrevious: 40 },
      { key: 'started', count: 380, percentOfPrevious: 95 },
      { key: 'completed', count: 300, percentOfPrevious: (300 / 380) * 100 },
      { key: 'analyzed', count: 290, percentOfPrevious: (290 / 300) * 100 },
    ]);
  });

  it("oldingi bosqich 0 bo'lganda nol bo'linish qilmaydi — foiz null bo'ladi", () => {
    const steps = buildFunnelSteps({
      linkViews: 0,
      registered: 0,
      started: 5,
      completed: 2,
      analyzed: 1,
    });

    expect(steps[0]).toEqual({ key: 'linkViews', count: 0, percentOfPrevious: null });
    expect(steps[1]).toEqual({ key: 'registered', count: 0, percentOfPrevious: null });
    // `started`ning oldingisi (`registered`) 0 — bo'linish qilinmaydi, natija Infinity/NaN emas.
    expect(steps[2]).toEqual({ key: 'started', count: 5, percentOfPrevious: null });
    expect(steps[2]?.percentOfPrevious).not.toBeNaN();
    expect(Number.isFinite(steps[2]?.percentOfPrevious ?? 0)).toBe(true);
  });

  it('barcha bosqichlar 0 bo\'lsa ham yiqilmaydi', () => {
    const steps = buildFunnelSteps({
      linkViews: 0,
      registered: 0,
      started: 0,
      completed: 0,
      analyzed: 0,
    });

    for (const step of steps) {
      expect(step.count).toBe(0);
    }
    expect(steps.every((step) => step.percentOfPrevious === null)).toBe(true);
  });

  it('to‘liq konversiyada (100%) foizni to‘g‘ri hisoblaydi', () => {
    const steps = buildFunnelSteps({
      linkViews: 50,
      registered: 50,
      started: 50,
      completed: 50,
      analyzed: 50,
    });

    expect(steps.slice(1).every((step) => step.percentOfPrevious === 100)).toBe(true);
  });
});
