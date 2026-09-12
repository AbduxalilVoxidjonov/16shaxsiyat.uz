import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { StudentSummaryCards } from './StudentSummaryCards';
import type { Mbti16Result } from '../model/profileTypes';
import { CORE_TEST_CODES } from '../model/testBattery';

/** Ikkala test ham to'liq batareyani simulyatsiya qiladi — bu faylning maqsadi "shaxsiyat
 * tipi" kartasining ICHKI mantig'i, metodika mavjudligi emas (u alohida test qilinadi —
 * `StudentProfilePage.test.tsx`). */
const ALL_TESTS_PRESENT = new Set(CORE_TEST_CODES);

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
        presentTests={ALL_TESTS_PRESENT}
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
        presentTests={ALL_TESTS_PRESENT}
      />,
    );

    const code = screen.getByText('INTJ');
    expect(code.className).toContain('text-2xl');
    expect(screen.queryByText('Loyihachi')).not.toBeInTheDocument();
  });
});

/**
 * P52 jonli xato tuzatish (2026-09-12): metodika sessiyada UMUMAN yo'q bo'lsa karta
 * umuman chizilmasin — natija hali hisoblanmagan holatdan ("Hali natija yo'q") farqli.
 */
describe('StudentSummaryCards — presentTests bilan mavjudlik nazorati', () => {
  it('presentTests bo\'sh bo\'lsa hech qanday karta chizilmaydi', () => {
    const { container } = render(
      <StudentSummaryCards
        mbti16={MBTI16}
        bigFive={null}
        riasec={null}
        activityIndex={null}
        activityLevelText={null}
        presentTests={new Set()}
      />,
    );

    expect(container).toBeEmptyDOMElement();
  });

  it('faqat sessiyada BOR metodikalar kartasi chiqadi (aralash holat)', () => {
    render(
      <StudentSummaryCards
        mbti16={MBTI16}
        bigFive={null}
        riasec={null}
        activityIndex={null}
        activityLevelText={null}
        presentTests={new Set(['MBTI16'])}
      />,
    );

    expect(screen.getByText('INTJ')).toBeInTheDocument();
    // Faqat BITTA karta chizilishi kerak — Big Five/RIASEC/Aktivlik kartalari yo'q.
    const cardLabels = ['Yetuklik indeksi', 'Aktivlik indeksi', 'Kasb qiziqishlari'];
    for (const label of cardLabels) {
      expect(screen.queryByText(label)).not.toBeInTheDocument();
    }
  });
});
