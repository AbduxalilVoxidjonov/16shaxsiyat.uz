import { describe, expect, it } from 'vitest';
import { render } from '@testing-library/react';
import axe from 'axe-core';
import { AxisBar } from './AxisBar';
import { PersonalityRadar } from './PersonalityRadar';
import { RiasecChart } from './RiasecChart';
import { ActivityBars } from './ActivityBars';
import { IndexGauge } from './IndexGauge';
import { ReliabilityBadge } from './ReliabilityBadge';
import { AiReportView, type AiReportSections } from './AiReportView';

/**
 * P26 DoD: "`axe` a11y tekshiruvi buzilishsiz". Loyihada `jest-axe`/`vitest-axe` o'ramchisi
 * yo'q edi — `axe-core`ning o'zi `eslint-plugin-jsx-a11y` orqali allaqachon (tranzitiv)
 * mavjud bo'lgani uchun `package.json`ga `devDependencies`ga aniq qo'shildi (versiyasi
 * `package-lock.json`dagi bilan bir xil, tarmoqsiz o'rnatildi) va shu yerda to'g'ridan-to'g'ri
 * `axe.run()` bilan ishlatiladi. jsdom to'liq CSS layout dvigatelini amalga oshirmagani uchun
 * ba'zi qoidalar (`color-contrast` kabi layout/piksel talab qiladiganlari) `disableRules`da
 * o'chirilgan — ular real brauzerda emas, shu sabab jsdom'da ma'nosiz signal berardi;
 * qolgan barcha strukturaviy/semantik qoidalar (ARIA, jadval, forma yorliqlari va h.k.) ishlaydi.
 */
const JSDOM_UNSUPPORTED_RULES = ['color-contrast'];

async function expectNoAxeViolations(container: Element): Promise<void> {
  const results = await axe.run(container, {
    rules: Object.fromEntries(JSDOM_UNSUPPORTED_RULES.map((rule) => [rule, { enabled: false }])),
  });
  expect(results.violations).toEqual([]);
}

const FULL_AI_SECTIONS: AiReportSections = {
  summary: 'Umumiy xulosa matni',
  strengths: [{ title: 'Kuchli tomon', description: 'Tavsif', evidence: 'Asos' }],
  attentionFlags: [{ code: 'LOW_MOTIVATION', message: 'Motivatsiya past', severity: 'attention' }],
  disclaimer: 'Bu tahlil tashxis emas.',
};

describe('widgets — axe a11y', () => {
  it('AxisBar', async () => {
    const { container } = render(<AxisBar axisCode="EI" pct={28.3} letter="I" borderline={false} />);
    await expectNoAxeViolations(container);
  });

  it('PersonalityRadar', async () => {
    const { container } = render(
      <PersonalityRadar
        openness={{ pct: 70, level: 'Yuqori' }}
        conscientiousness={{ pct: 77.5, level: 'Yuqori' }}
        extraversion={{ pct: 35, level: 'Past' }}
        agreeableness={{ pct: 62.5, level: 'Yuqori' }}
        stabilityPct={70}
      />,
    );
    await expectNoAxeViolations(container);
  });

  it('RiasecChart', async () => {
    const { container } = render(
      <RiasecChart
        types={{ R: 62, I: 88, A: 71, S: 40, E: 35, C: 48 }}
        resultCode="IRA"
        differentiation={53}
      />,
    );
    await expectNoAxeViolations(container);
  });

  it('ActivityBars', async () => {
    const { container } = render(
      <ActivityBars
        scales={{ MOT: 74, SELF: 68, SOCA: 52, ENG: 60 }}
        activityIndex={65.2}
        activityLevelText="O'rtacha faol"
      />,
    );
    await expectNoAxeViolations(container);
  });

  it('IndexGauge', async () => {
    const { container } = render(
      <IndexGauge value={68.4} label="Yetuklik indeksi" levelText="Yaxshi" />,
    );
    await expectNoAxeViolations(container);
  });

  it('ReliabilityBadge', async () => {
    const { container } = render(<ReliabilityBadge flag="Reliable" score={82.5} />);
    await expectNoAxeViolations(container);
  });

  it('AiReportView', async () => {
    const { container } = render(<AiReportView sections={FULL_AI_SECTIONS} />);
    await expectNoAxeViolations(container);
  });

  it("AiReportView — bo'sh ma'lumot", async () => {
    const { container } = render(<AiReportView sections={{}} />);
    await expectNoAxeViolations(container);
  });
});
