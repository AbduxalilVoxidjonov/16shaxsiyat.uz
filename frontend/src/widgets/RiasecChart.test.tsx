import { describe, expect, it } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import { RiasecChart } from './RiasecChart';

const ZERO_TYPES = { R: 0, I: 0, ART: 0, SOC: 0, ENT: 0, CONV: 0 };
const FULL_TYPES = { R: 100, I: 100, ART: 100, SOC: 100, ENT: 100, CONV: 100 };

describe('RiasecChart', () => {
  it('Holland kodi va raqamli qiymatlarni ko\'rsatadi', () => {
    render(
      <RiasecChart
        types={{ R: 62, I: 88, ART: 71, SOC: 40, ENT: 35, CONV: 48 }}
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
        types={{ R: 50, I: 55, ART: 45, SOC: 48, ENT: 52, CONV: 50 }}
        resultCode="IRS"
        differentiation={10}
      />,
    );
    expect(screen.getByText(/hali aniq shakllanmagan/)).toBeInTheDocument();
  });

  it('yashirin jadval alternativi mavjud', () => {
    render(
      <RiasecChart
        types={{ R: 62, I: 88, ART: 71, SOC: 40, ENT: 35, CONV: 48 }}
        resultCode="IRA"
        differentiation={53}
      />,
    );
    const table = screen.getByTestId('riasec-chart-table');
    expect(table.className).toContain('sr-only');
    expect(within(table).getAllByRole('row').length).toBeGreaterThan(1);
  });
});
