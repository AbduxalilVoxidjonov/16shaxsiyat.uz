import { describe, expect, it } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import { RiasecChart } from './RiasecChart';

const ZERO_TYPES = { R: 0, I: 0, A: 0, S: 0, E: 0, C: 0 };
const FULL_TYPES = { R: 100, I: 100, A: 100, S: 100, E: 100, C: 100 };

describe('RiasecChart', () => {
  it('Holland kodi va raqamli qiymatlarni ko\'rsatadi', () => {
    render(
      <RiasecChart
        types={{ R: 62, I: 88, A: 71, S: 40, E: 35, C: 48 }}
        resultCode="IRA"
        differentiation={53}
      />,
    );
    expect(within(screen.getByTestId('riasec-chart-code')).getByText(/IRA/)).toBeInTheDocument();
    expect(within(screen.getByTestId('riasec-chart-summary')).getByText('88.0%')).toBeInTheDocument();
    expect(screen.getByText(/farqlanish darajasi 53\.0/)).toBeInTheDocument();
  });

  it('0% chegaraviy qiymatda yiqilmaydi', () => {
    render(<RiasecChart types={ZERO_TYPES} resultCode="RIA" differentiation={0} />);
    expect(
      within(screen.getByTestId('riasec-chart-summary')).getAllByText('0.0%').length,
    ).toBe(6);
  });

  it('100% chegaraviy qiymatda yiqilmaydi', () => {
    render(<RiasecChart types={FULL_TYPES} resultCode="RIA" differentiation={0} />);
    expect(
      within(screen.getByTestId('riasec-chart-summary')).getAllByText('100.0%').length,
    ).toBe(6);
  });

  it("differensiatsiya 20 dan past bo'lsa shakllanmaganlik matni chiqadi", () => {
    render(
      <RiasecChart
        types={{ R: 50, I: 55, A: 45, S: 48, E: 52, C: 50 }}
        resultCode="IRS"
        differentiation={10}
      />,
    );
    expect(screen.getByText(/hali aniq shakllanmagan/)).toBeInTheDocument();
  });

  it('yashirin jadval alternativi mavjud', () => {
    render(
      <RiasecChart
        types={{ R: 62, I: 88, A: 71, S: 40, E: 35, C: 48 }}
        resultCode="IRA"
        differentiation={53}
      />,
    );
    const table = screen.getByTestId('riasec-chart-table');
    // Jadval `sr-only` O'RAM ichida (`VisuallyHidden`) — `sr-only` ni jadvalning
    // O'ZIGA berib bo'lmaydi: u holda jadval eni sahifadan chiqib ketadi (P30-5,
    // izohi `shared/ui/VisuallyHidden.tsx` da).
    expect(table.parentElement?.className).toContain('sr-only');
    expect(within(table).getAllByRole('row').length).toBeGreaterThan(1);
  });
});
