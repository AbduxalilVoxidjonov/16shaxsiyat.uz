import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { StudentSummaryCards } from './StudentSummaryCards';
import type { Mbti16Result } from '../model/profileTypes';

/**
 * Egasining talabi (2026-09-03): "qisqartirib yozilgan 16 ta shaxsiyatni to'liq nomi bilan
 * chiqar". Tip nomi ASOSIY, 4 harfli kod ikkinchi darajali (`ART`/`Artistik` naqshi).
 * Nomlar `type-catalog.json` dan keladi (mustaqil yozilgan — `CLAUDE.md` 6a-band).
 */
const MBTI16: Mbti16Result = {
  resultCode: 'INTJ',
  typeName: 'Loyihachi',
  axes: {
    EI: { pct: 28.3, letter: 'I', borderline: false },
    SN: { pct: 71.6, letter: 'N', borderline: false },
    TF: { pct: 33.3, letter: 'T', borderline: false },
    JP: { pct: 64.1, letter: 'J', borderline: false },
  },
  borderlineAxes: [],
};

describe('StudentSummaryCards — shaxsiyat tipi kartasi', () => {
  it("to'liq nomni ASOSIY qiymat, kodni izoh sifatida ko'rsatadi", () => {
    render(
      <StudentSummaryCards
        mbti16={MBTI16}
        bigFive={null}
        riasec={null}
        activityIndex={null}
        activityLevelText={null}
      />,
    );

    const name = screen.getByText('Loyihachi');
    const code = screen.getByText('INTJ');

    expect(name.className).toContain('text-2xl');
    expect(code.className).toContain('text-xs');
  });

  it("typeName bo'sh bo'lsa FAQAT kodni ko'rsatadi va yiqilmaydi", () => {
    render(
      <StudentSummaryCards
        mbti16={{ ...MBTI16, typeName: '' }}
        bigFive={null}
        riasec={null}
        activityIndex={null}
        activityLevelText={null}
      />,
    );

    const code = screen.getByText('INTJ');
    expect(code.className).toContain('text-2xl');
    expect(screen.queryByText('Loyihachi')).not.toBeInTheDocument();
  });
});
