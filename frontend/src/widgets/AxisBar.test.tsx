import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { AxisBar } from './AxisBar';

describe('AxisBar', () => {
  it('0% — birinchi qutb chekkasida raqamli qiymatni ko\'rsatadi', () => {
    render(<AxisBar axisCode="EI" pct={0} letter="I" borderline={false} />);
    expect(screen.getByText('I — 0.0%')).toBeInTheDocument();
    expect(screen.getByRole('img')).toHaveAttribute('aria-label', expect.stringContaining('0.0'));
  });

  it('100% — ikkinchi qutb chekkasida raqamli qiymatni ko\'rsatadi', () => {
    render(<AxisBar axisCode="EI" pct={100} letter="E" borderline={false} />);
    expect(screen.getByText('E — 100.0%')).toBeInTheDocument();
  });

  it("50% (borderline) — muvozanat belgisi ko'rsatiladi", () => {
    render(<AxisBar axisCode="JP" pct={50} letter="J" borderline />);
    expect(screen.getByText('J — 50.0%')).toBeInTheDocument();
    expect(screen.getByText('Muvozanat')).toBeInTheDocument();
  });

  it('borderline=false bo\'lsa muvozanat belgisi chiqmaydi', () => {
    render(<AxisBar axisCode="SN" pct={71.6} letter="N" borderline={false} />);
    expect(screen.queryByText('Muvozanat')).not.toBeInTheDocument();
  });

  it('har diagramma uchun yashirin jadval alternativi mavjud', () => {
    render(<AxisBar axisCode="TF" pct={33.3} letter="T" borderline={false} />);
    const table = screen.getByRole('table', { hidden: true });
    expect(table).toBeInTheDocument();
    // Jadval `sr-only` O'RAM ichida (`VisuallyHidden`) — `sr-only` ni jadvalning
    // O'ZIGA berib bo'lmaydi: u holda jadval eni sahifadan chiqib ketadi (P30-5,
    // izohi `shared/ui/VisuallyHidden.tsx` da).
    expect(table.parentElement?.className).toContain('sr-only');
  });

  it('diapazondan tashqari (manfiy/100dan katta) qiymatlarni kesib qo\'yadi', () => {
    render(<AxisBar axisCode="EI" pct={-15} letter="I" borderline={false} />);
    expect(screen.getByText('I — 0.0%')).toBeInTheDocument();
  });
});
