import { useState } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { LongTextQuestion, type LongTextQuestionProps } from './LongTextQuestion';
import type { BranchingQuestion } from '@/shared/api/branchingTypes';

const BASE_QUESTION: BranchingQuestion = {
  id: 'q1',
  code: 'S3-Q02',
  order: 7,
  text: "Do'stingiz haqida qisqacha yozing",
  type: 'LongText',
  isRequired: false,
  options: null,
  currentValue: null,
  sectionId: null,
  placeholder: null,
  inputPattern: null,
  maxLength: 20,
  minSelections: null,
  maxSelections: null,
  visibility: null,
  currentText: null,
  currentValues: null,
};

function renderQuestion(overrides: Partial<LongTextQuestionProps> = {}) {
  const onAnswer = vi.fn();
  render(
    <LongTextQuestion question={BASE_QUESTION} value={null} invalid={false} onAnswer={onAnswer} {...overrides} />,
  );
  return { onAnswer };
}

/** Typing testlari uchun — controlled `textarea` mahalliy holatda javobni ushlab turadi. */
function ControlledLongTextQuestion({ onAnswer }: { onAnswer: (payload: { text: string }) => void }) {
  const [value, setValue] = useState<string | null>(null);
  return (
    <LongTextQuestion
      question={BASE_QUESTION}
      value={value}
      invalid={false}
      onAnswer={(payload) => {
        onAnswer(payload);
        setValue(payload.text);
      }}
    />
  );
}

describe('LongTextQuestion', () => {
  it("savol matnini ko'rsatadi va textarea bilan bog'laydi", () => {
    renderQuestion();
    expect(screen.getByLabelText(/Do'stingiz haqida/)).toBeInTheDocument();
    expect(screen.getByLabelText(/Do'stingiz haqida/).tagName).toBe('TEXTAREA');
  });

  it("belgi hisoblagichi boshida '0 / maxLength' ko'rsatadi", () => {
    renderQuestion();
    expect(screen.getByText('0 / 20')).toBeInTheDocument();
  });

  it('matn kiritilganda hisoblagich yangilanadi va onAnswer chaqiriladi', async () => {
    const user = userEvent.setup();
    const onAnswer = vi.fn();
    render(<ControlledLongTextQuestion onAnswer={onAnswer} />);

    await user.type(screen.getByLabelText(/Do'stingiz haqida/), 'Salom');

    expect(onAnswer).toHaveBeenLastCalledWith({ text: 'Salom' });
    expect(screen.getByText('5 / 20')).toBeInTheDocument();
  });

  it("invalid=true bo'lsa xato xabari ko'rsatiladi", () => {
    renderQuestion({ invalid: true });
    expect(screen.getByRole('alert')).toHaveTextContent('Bu savolga javob bering.');
  });

  it("invalid=false bo'lsa xato xabari ko'rinmaydi", () => {
    renderQuestion({ invalid: false });
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('Enter yangi qator yaratadi — savol ilgarilamaydi (textarea odatiy xatti-harakati)', async () => {
    const user = userEvent.setup();
    const onAnswer = vi.fn();
    render(<ControlledLongTextQuestion onAnswer={onAnswer} />);

    const textarea = screen.getByLabelText(/Do'stingiz haqida/);
    await user.type(textarea, 'A{Enter}B');

    expect(onAnswer).toHaveBeenLastCalledWith({ text: 'A\nB' });
  });

  it("maxLength HTML atributi sifatida qo'llanadi", () => {
    renderQuestion();
    expect(screen.getByLabelText(/Do'stingiz haqida/)).toHaveAttribute('maxlength', '20');
  });
});
