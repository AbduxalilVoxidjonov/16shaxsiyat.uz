import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { TestResultsSummary } from './TestResultsSummary';
import type { TestSummaryRow } from '../model/testSummary';

function row(overrides: Partial<TestSummaryRow> = {}): TestSummaryRow {
  return {
    testCode: 'MBTI16',
    nameUz: '16 tipli shaxsiyat modeli',
    state: 'scored',
    resultCode: 'INTJ',
    resultName: 'Loyihachi',
    index: null,
    indexKind: null,
    ...overrides,
  } as TestSummaryRow;
}

describe('TestResultsSummary', () => {
  // Egasining 2026-09-03 talabi: 4 harfli kod o'zi hech narsa anglatmaydi, to'liq nom
  // ko'rinishi kerak. Ilgari `"INTJ · Loyihachi"` bo'lib, kod BIRINCHI va ikkalasi bir
  // xil vaznda edi.
  it("tip nomi asosiy, kod ikkinchi darajali bo'lib ko'rsatiladi", () => {
    render(<TestResultsSummary rows={[row()]} />);

    const name = screen.getByText('Loyihachi');
    const code = screen.getByText('INTJ');

    expect(name).toHaveClass('font-medium');
    expect(code).toHaveClass('text-neutral-400');
    // Kod nomdan KEYIN keladi — nom birinchi o'qiladi.
    expect(name.compareDocumentPosition(code) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it("nom yo'q bo'lsa (masalan RIASEC Holland kodi) faqat kod ko'rsatiladi, takrorlanmaydi", () => {
    render(<TestResultsSummary rows={[row({ testCode: 'RIASEC', resultCode: 'IRA', resultName: null })]} />);

    expect(screen.getAllByText('IRA')).toHaveLength(1);
    expect(screen.getByText('IRA')).toHaveClass('font-medium');
  });
});
