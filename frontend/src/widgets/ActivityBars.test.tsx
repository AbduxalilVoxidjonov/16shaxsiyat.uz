import { describe, expect, it } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import { ActivityBars } from './ActivityBars';

describe('ActivityBars', () => {
  it('4 shkala va umumiy indeksni ko\'rsatadi', () => {
    render(
      <ActivityBars
        scales={{ MOT: 74, SELF: 68, SOCA: 52, ENG: 60 }}
        activityIndex={65.2}
        activityLevelText="O'rtacha faol"
      />,
    );
    expect(
      within(screen.getByTestId('activity-bar-MOT')).getByText("Ta'lim motivatsiyasi"),
    ).toBeInTheDocument();
    expect(within(screen.getByTestId('activity-bar-MOT')).getByText('74.0%')).toBeInTheDocument();
    const gaugeValue = screen.getByTestId('index-gauge-value');
    expect(within(gaugeValue).getByText('65.2')).toBeInTheDocument();
    expect(within(gaugeValue).getByText("O'rtacha faol")).toBeInTheDocument();
  });

  it('0% chegaraviy qiymatda yiqilmaydi', () => {
    render(
      <ActivityBars
        scales={{ MOT: 0, SELF: 0, SOCA: 0, ENG: 0 }}
        activityIndex={0}
        activityLevelText="Passiv"
      />,
    );
    for (const code of ['MOT', 'SELF', 'SOCA', 'ENG']) {
      expect(within(screen.getByTestId(`activity-bar-${code}`)).getByText('0.0%')).toBeInTheDocument();
    }
  });

  it('100% chegaraviy qiymatda yiqilmaydi', () => {
    render(
      <ActivityBars
        scales={{ MOT: 100, SELF: 100, SOCA: 100, ENG: 100 }}
        activityIndex={100}
        activityLevelText="Juda faol"
      />,
    );
    for (const code of ['MOT', 'SELF', 'SOCA', 'ENG']) {
      expect(
        within(screen.getByTestId(`activity-bar-${code}`)).getByText('100.0%'),
      ).toBeInTheDocument();
    }
  });

  it('yashirin jadval alternativi mavjud', () => {
    render(
      <ActivityBars
        scales={{ MOT: 74, SELF: 68, SOCA: 52, ENG: 60 }}
        activityIndex={65.2}
        activityLevelText="O'rtacha faol"
      />,
    );
    const table = screen.getByTestId('activity-bars-table');
    expect(table.className).toContain('sr-only');
  });
});
