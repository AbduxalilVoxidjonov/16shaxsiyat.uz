import { describe, expect, it } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import { IndexGauge } from './IndexGauge';

describe('IndexGauge', () => {
  it('0 qiymatida raqam va daraja matnini ko\'rsatadi', () => {
    render(<IndexGauge value={0} label="Yetuklik indeksi" levelText="Shakllanish bosqichida" />);
    const value = screen.getByTestId('index-gauge-value');
    expect(within(value).getByText('0.0')).toBeInTheDocument();
    expect(within(value).getByText('Shakllanish bosqichida')).toBeInTheDocument();
    expect(screen.getByRole('img')).toHaveAttribute('aria-label', expect.stringContaining('0.0'));
  });

  it('50 qiymatida raqamni ko\'rsatadi', () => {
    render(<IndexGauge value={50} label="Aktivlik indeksi" levelText="O'rtacha faol" />);
    expect(
      within(screen.getByTestId('index-gauge-value')).getByText('50.0'),
    ).toBeInTheDocument();
  });

  it('100 qiymatida raqamni ko\'rsatadi', () => {
    render(<IndexGauge value={100} label="Aktivlik indeksi" levelText="Juda faol" />);
    expect(
      within(screen.getByTestId('index-gauge-value')).getByText('100.0'),
    ).toBeInTheDocument();
  });

  it('diapazondan tashqari qiymatni [0,100]ga kesadi', () => {
    render(<IndexGauge value={140} label="Yetuklik indeksi" levelText="Yuqori" />);
    expect(
      within(screen.getByTestId('index-gauge-value')).getByText('100.0'),
    ).toBeInTheDocument();
  });

  it('yashirin jadval alternativi mavjud', () => {
    render(<IndexGauge value={68.4} label="Yetuklik indeksi" levelText="Yaxshi" />);
    const table = screen.getByTestId('index-gauge-table');
    // Jadval `sr-only` O'RAM ichida (`VisuallyHidden`) — `sr-only` ni jadvalning
    // O'ZIGA berib bo'lmaydi: u holda jadval eni sahifadan chiqib ketadi (P30-5,
    // izohi `shared/ui/VisuallyHidden.tsx` da).
    expect(table.parentElement?.className).toContain('sr-only');
    expect(within(table).getByText('68.4')).toBeInTheDocument();
  });
});
