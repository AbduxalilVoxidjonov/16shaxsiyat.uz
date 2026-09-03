import { describe, expect, it } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import { PersonalityRadar } from './PersonalityRadar';
import { classifyBigFiveLevel } from './personalityRadarLevel';

const FACTOR = { pct: 50, level: "O'rtacha" };

describe('PersonalityRadar', () => {
  it("'N' o'rniga 'Emotsional barqarorlik' ko'rsatiladi", () => {
    render(
      <PersonalityRadar
        openness={FACTOR}
        conscientiousness={FACTOR}
        extraversion={FACTOR}
        agreeableness={FACTOR}
        stabilityPct={70}
      />,
    );
    const summary = screen.getByTestId('personality-radar-summary');
    expect(within(summary).getByText('Emotsional barqarorlik')).toBeInTheDocument();
    expect(screen.queryByText(/neyrotizm/i)).not.toBeInTheDocument();
    expect(within(summary).queryByText(/^N$/)).not.toBeInTheDocument();
  });

  it('har omil yonida raqamli qiymat va daraja matni ko\'rsatadi', () => {
    render(
      <PersonalityRadar
        openness={{ pct: 70, level: 'Yuqori' }}
        conscientiousness={{ pct: 77.5, level: 'Yuqori' }}
        extraversion={{ pct: 35, level: 'Past' }}
        agreeableness={{ pct: 62.5, level: 'Yuqori' }}
        stabilityPct={70}
      />,
    );
    const summary = screen.getByTestId('personality-radar-summary');
    expect(within(summary).getByText('77.5%')).toBeInTheDocument();
    expect(within(summary).getAllByText(/Yuqori/).length).toBeGreaterThan(0);
  });

  it('yashirin jadval alternativi mavjud', () => {
    render(
      <PersonalityRadar
        openness={FACTOR}
        conscientiousness={FACTOR}
        extraversion={FACTOR}
        agreeableness={FACTOR}
        stabilityPct={50}
      />,
    );
    const table = screen.getByTestId('personality-radar-table');
    // Jadval `sr-only` O'RAM ichida (`VisuallyHidden`) — `sr-only` ni jadvalning
    // O'ZIGA berib bo'lmaydi: u holda jadval eni sahifadan chiqib ketadi (P30-5,
    // izohi `shared/ui/VisuallyHidden.tsx` da).
    expect(table.parentElement?.className).toContain('sr-only');
    expect(within(table).getAllByRole('row').length).toBeGreaterThan(1);
  });
});

describe('classifyBigFiveLevel', () => {
  const t = (key: string) => key.split('.').pop() ?? key;

  it('chegaraviy qiymatlarni to\'g\'ri tasniflaydi (docs/03, 3.2)', () => {
    expect(classifyBigFiveLevel(0, t)).toBe('veryLow');
    expect(classifyBigFiveLevel(20, t)).toBe('veryLow');
    expect(classifyBigFiveLevel(21, t)).toBe('low');
    expect(classifyBigFiveLevel(40, t)).toBe('low');
    expect(classifyBigFiveLevel(41, t)).toBe('medium');
    expect(classifyBigFiveLevel(50, t)).toBe('medium');
    expect(classifyBigFiveLevel(60, t)).toBe('medium');
    expect(classifyBigFiveLevel(61, t)).toBe('high');
    expect(classifyBigFiveLevel(80, t)).toBe('high');
    expect(classifyBigFiveLevel(81, t)).toBe('veryHigh');
    expect(classifyBigFiveLevel(100, t)).toBe('veryHigh');
  });
});
