import { useState } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TextQuestion, type TextQuestionProps } from './TextQuestion';
import type { BranchingQuestion } from '@/shared/api/branchingTypes';

const BASE_QUESTION: BranchingQuestion = {
  id: 'q1',
  code: 'S1-Q03',
  order: 3,
  text: 'Telefon raqamingiz',
  type: 'Phone',
  isRequired: true,
  options: null,
  currentValue: null,
  sectionId: null,
  placeholder: null,
  inputPattern: null,
  maxLength: null,
  minSelections: null,
  maxSelections: null,
  visibility: null,
  currentText: null,
  currentValues: null,
};

function renderQuestion(overrides: Partial<TextQuestionProps> = {}) {
  const onAnswer = vi.fn();
  const onAdvance = vi.fn();
  render(
    <TextQuestion
      question={BASE_QUESTION}
      value={null}
      invalid={false}
      onAnswer={onAnswer}
      onAdvance={onAdvance}
      {...overrides}
    />,
  );
  return { onAnswer, onAdvance };
}

/**
 * Yozish (typing) testlari uchun — HAQIQIY `TestPage` kabi javobni mahalliy holatda ushlab
 * turadi, aks holda controlled `<input>` har `onAnswer` chaqiruvidan keyin `value` propi
 * o'zgarmagani sabab har harfda bo'shliqqa qaytib ketardi (React controlled input).
 */
function ControlledTextQuestion({
  onAnswer,
  question = BASE_QUESTION,
}: {
  onAnswer: (payload: { text: string }) => void;
  question?: BranchingQuestion;
}) {
  const [value, setValue] = useState<string | null>(null);
  return (
    <TextQuestion
      question={question}
      value={value}
      invalid={false}
      onAnswer={(payload) => {
        onAnswer(payload);
        setValue(payload.text);
      }}
      onAdvance={vi.fn()}
    />
  );
}

describe('TextQuestion', () => {
  it("savol matnini ko'rsatadi va matn kiritish maydoni bilan bog'laydi (label→input)", () => {
    renderQuestion();
    expect(screen.getByLabelText(/Telefon raqamingiz/)).toBeInTheDocument();
  });

  it('Phone turida inputMode="tel" va standart placeholder qo\'llanadi', () => {
    renderQuestion();
    const input = screen.getByLabelText(/Telefon raqamingiz/);
    expect(input).toHaveAttribute('inputmode', 'tel');
    expect(input).toHaveAttribute('placeholder', '+998 90 123 45 67');
  });

  it('ShortText turida inputMode berilmaydi', () => {
    renderQuestion({ question: { ...BASE_QUESTION, type: 'ShortText', text: 'F.I.Sh.' } });
    expect(screen.getByLabelText(/F\.I\.Sh\./)).not.toHaveAttribute('inputmode');
  });

  it('matn kiritilganda onAnswer to\'liq matn bilan chaqiriladi', async () => {
    const user = userEvent.setup();
    const onAnswer = vi.fn();
    render(<ControlledTextQuestion onAnswer={onAnswer} />);

    await user.type(screen.getByLabelText(/Telefon raqamingiz/), '+998901234567');

    expect(onAnswer).toHaveBeenLastCalledWith({ text: '+998901234567' });
  });

  it("noto'g'ri telefon formatida jonli o'zbekcha xato ko'rsatiladi", async () => {
    const user = userEvent.setup();
    render(<ControlledTextQuestion onAnswer={vi.fn()} />);

    await user.type(screen.getByLabelText(/Telefon raqamingiz/), '123');

    expect(screen.getByRole('alert')).toHaveTextContent(
      "To'g'ri telefon raqam kiriting (masalan: +998 90 123 45 67).",
    );
  });

  it("to'g'ri formatda xato ko'rinmaydi", async () => {
    const user = userEvent.setup();
    render(<ControlledTextQuestion onAnswer={vi.fn()} />);

    await user.type(screen.getByLabelText(/Telefon raqamingiz/), '+998901234567');

    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('Enter bosilganda onAdvance chaqiriladi', async () => {
    const user = userEvent.setup();
    const { onAdvance } = renderQuestion();

    screen.getByLabelText(/Telefon raqamingiz/).focus();
    await user.keyboard('{Enter}');

    expect(onAdvance).toHaveBeenCalledTimes(1);
  });

  it("invalid=true bo'lsa xato xabari ko'rsatiladi", () => {
    renderQuestion({ invalid: true });
    expect(screen.getByRole('alert')).toHaveTextContent('Bu savolga javob bering.');
  });

  it("invalid=false bo'lsa xato xabari ko'rinmaydi", () => {
    renderQuestion({ invalid: false });
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it("maxLength qo'llanadi (savolda belgilangan bo'lsa)", () => {
    renderQuestion({ question: { ...BASE_QUESTION, maxLength: 20 } });
    expect(screen.getByLabelText(/Telefon raqamingiz/)).toHaveAttribute('maxlength', '20');
  });

  it("majburiy savolda matn oxirida qizil * (aria-hidden) va input'da aria-required", () => {
    const { container } = render(
      <TextQuestion
        question={BASE_QUESTION}
        value={null}
        invalid={false}
        onAnswer={vi.fn()}
        onAdvance={vi.fn()}
      />,
    );
    expect(screen.getByRole('textbox', { name: /Telefon raqamingiz$/ })).toHaveAttribute(
      'aria-required',
      'true',
    );
    const mark = container.querySelector('label [data-required-mark]');
    expect(mark).toHaveAttribute('aria-hidden', 'true');
  });

  it("ixtiyoriy savolda * ko'rsatilmaydi", () => {
    const { container } = render(
      <TextQuestion
        question={{ ...BASE_QUESTION, isRequired: false }}
        value={null}
        invalid={false}
        onAnswer={vi.fn()}
        onAdvance={vi.fn()}
      />,
    );
    expect(container.querySelector('[data-required-mark]')).toBeNull();
    expect(screen.getByLabelText(/Telefon raqamingiz/)).not.toHaveAttribute('aria-required');
  });
});
