import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { LikertQuestion } from './LikertQuestion';
import type { PublicQuestion, PublicScaleLabel } from '@/shared/api/types';

const SCALE_LABELS: PublicScaleLabel[] = [
  { value: 1, label: "Umuman qo'shilmayman" },
  { value: 2, label: "Qo'shilmayman" },
  { value: 3, label: 'Bilmadim' },
  { value: 4, label: "Qo'shilaman" },
  { value: 5, label: "To'liq qo'shilaman" },
];

const QUESTION: PublicQuestion = {
  id: 'q1',
  code: 'B5-Q01',
  order: 11,
  text: "Yangi g'oyalarni sinab ko'rishni yaxshi ko'raman",
  type: 'Likert5',
  isRequired: true,
  options: null,
  currentValue: null,
};

function renderQuestion(overrides: Partial<Parameters<typeof LikertQuestion>[0]> = {}) {
  const onAnswer = vi.fn();
  const onAdvance = vi.fn();
  render(
    <LikertQuestion
      question={QUESTION}
      scaleLabels={SCALE_LABELS}
      value={null}
      invalid={false}
      onAnswer={onAnswer}
      onAdvance={onAdvance}
      {...overrides}
    />,
  );
  return { onAnswer, onAdvance };
}

describe('LikertQuestion', () => {
  it("savol matni va 5 ta variantni ko'rsatadi", () => {
    renderQuestion();
    expect(screen.getByText(QUESTION.text)).toBeInTheDocument();
    for (const option of SCALE_LABELS) {
      expect(screen.getByRole('radio', { name: option.label })).toBeInTheDocument();
    }
  });

  it('variantga bosilganda onAnswer chaqiriladi', async () => {
    const user = userEvent.setup();
    const { onAnswer } = renderQuestion();

    await user.click(screen.getByRole('radio', { name: "Qo'shilaman" }));

    expect(onAnswer).toHaveBeenCalledWith(4);
  });

  it("tanlangan variant 'checked' bo'lib ko'rsatiladi", () => {
    renderQuestion({ value: 3 });
    expect(screen.getByRole('radio', { name: 'Bilmadim' })).toBeChecked();
    expect(screen.getByRole('radio', { name: "Qo'shilaman" })).not.toBeChecked();
  });

  it("klaviaturada 1..5 raqamlari to'g'ri qiymatni tanlaydi (CLAUDE.md MAXSUS DIQQAT 3-band)", async () => {
    const user = userEvent.setup();
    const { onAnswer } = renderQuestion();

    screen.getByRole('radiogroup').focus();
    await user.keyboard('4');

    expect(onAnswer).toHaveBeenCalledWith(4);
  });

  it('Enter bosilganda onAdvance chaqiriladi', async () => {
    const user = userEvent.setup();
    const { onAdvance } = renderQuestion();

    screen.getByRole('radiogroup').focus();
    await user.keyboard('{Enter}');

    expect(onAdvance).toHaveBeenCalledTimes(1);
  });

  it("0 yoki 6 kabi noto'g'ri raqamlarga e'tibor bermaydi", async () => {
    const user = userEvent.setup();
    const { onAnswer } = renderQuestion();

    screen.getByRole('radiogroup').focus();
    await user.keyboard('0');
    await user.keyboard('6');

    expect(onAnswer).not.toHaveBeenCalled();
  });

  it("invalid=true bo'lsa xato xabari ko'rsatiladi", () => {
    renderQuestion({ invalid: true });
    expect(screen.getByRole('alert')).toHaveTextContent('Bu savolga javob bering.');
  });

  it("invalid=false bo'lsa xato xabari ko'rinmaydi", () => {
    renderQuestion({ invalid: false });
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  // Ko'rinish daraja soniga moslashadi (`03-public-api.md` 6-bo'lim) — quyidagi uch holat
  // shkala/variant turlarining har biri uchun bittadan (P45 dizayn ishi).
  it("Likert7 — 7 ta daraja ham radio sifatida ko'rsatiladi", () => {
    const labels: PublicScaleLabel[] = Array.from({ length: 7 }, (_, index) => ({
      value: index + 1,
      label: `Daraja ${String(index + 1)}`,
    }));
    renderQuestion({ scaleLabels: labels, question: { ...QUESTION, type: 'Likert7' } });

    expect(screen.getAllByRole('radio')).toHaveLength(7);
    expect(screen.getByRole('radio', { name: 'Daraja 7' })).toBeInTheDocument();
  });

  it("Binary — ikki daraja o'z yorlig'i bilan ko'rsatiladi va tanlanadi", async () => {
    const user = userEvent.setup();
    const onAnswer = vi.fn();
    render(
      <LikertQuestion
        question={{ ...QUESTION, type: 'Binary' }}
        scaleLabels={[
          { value: 0, label: "Yo'q" },
          { value: 1, label: 'Ha' },
        ]}
        value={null}
        invalid={false}
        onAnswer={onAnswer}
        onAdvance={vi.fn()}
      />,
    );

    await user.click(screen.getByRole('radio', { name: 'Ha' }));

    expect(onAnswer).toHaveBeenCalledWith(1);
  });

  it("SingleChoice — options[] variant kartalari sifatida ko'rsatiladi", async () => {
    const user = userEvent.setup();
    const onAnswer = vi.fn();
    render(
      <LikertQuestion
        question={{
          ...QUESTION,
          type: 'SingleChoice',
          options: [
            { id: 'o2', text: 'Ikkinchi variant', value: 20, order: 2 },
            { id: 'o1', text: 'Birinchi variant', value: 10, order: 1 },
          ],
        }}
        // `SingleChoice` uchun server `scaleLabels`ni bermaydi (`null`).
        scaleLabels={[]}
        value={null}
        invalid={false}
        onAnswer={onAnswer}
        onAdvance={vi.fn()}
      />,
    );

    // `order` bo'yicha saralanadi — birinchi bo'lib "Birinchi variant" chiqadi.
    expect(screen.getAllByRole('radio').map((node) => node.getAttribute('value'))).toEqual([
      '10',
      '20',
    ]);

    await user.click(screen.getByRole('radio', { name: 'Ikkinchi variant' }));

    expect(onAnswer).toHaveBeenCalledWith(20);
  });
});
