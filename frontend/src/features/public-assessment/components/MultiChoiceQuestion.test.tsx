import { useState } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MultiChoiceQuestion, type MultiChoiceQuestionProps } from './MultiChoiceQuestion';
import type { BranchingQuestion } from '@/shared/api/branchingTypes';

const BASE_QUESTION: BranchingQuestion = {
  id: 'q1',
  code: 'S1-Q06',
  order: 6,
  text: 'Qaysi fanlarga qiziqasiz?',
  type: 'MultiChoice',
  isRequired: true,
  options: [
    { id: 'o1', text: 'Matematika', value: 1, order: 1 },
    { id: 'o2', text: 'Adabiyot', value: 2, order: 2 },
    { id: 'o3', text: 'Biologiya', value: 3, order: 3 },
    { id: 'o4', text: 'Tarix', value: 4, order: 4 },
  ],
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

function renderQuestion(overrides: Partial<MultiChoiceQuestionProps> = {}) {
  const onAnswer = vi.fn();
  render(
    <MultiChoiceQuestion question={BASE_QUESTION} value={[]} invalid={false} onAnswer={onAnswer} {...overrides} />,
  );
  return { onAnswer };
}

/** Bir nechta checkbox bosilishini sinash uchun — mahalliy holatda tanlovlarni ushlab turadi. */
function ControlledMultiChoiceQuestion({
  question = BASE_QUESTION,
  onAnswer,
}: {
  question?: BranchingQuestion;
  onAnswer: (payload: { selectedValues: number[] }) => void;
}) {
  const [value, setValue] = useState<number[]>([]);
  return (
    <MultiChoiceQuestion
      question={question}
      value={value}
      invalid={false}
      onAnswer={(payload) => {
        onAnswer(payload);
        setValue(payload.selectedValues);
      }}
    />
  );
}

describe('MultiChoiceQuestion', () => {
  it("savol matni va barcha variantlarni checkbox sifatida ko'rsatadi", () => {
    renderQuestion();
    for (const option of BASE_QUESTION.options ?? []) {
      expect(screen.getByRole('checkbox', { name: option.text })).toBeInTheDocument();
    }
  });

  it('variant bosilganda onAnswer selectedValues bilan chaqiriladi', async () => {
    const user = userEvent.setup();
    const onAnswer = vi.fn();
    render(<ControlledMultiChoiceQuestion onAnswer={onAnswer} />);

    await user.click(screen.getByRole('checkbox', { name: 'Adabiyot' }));
    expect(onAnswer).toHaveBeenLastCalledWith({ selectedValues: [2] });

    await user.click(screen.getByRole('checkbox', { name: 'Tarix' }));
    expect(onAnswer).toHaveBeenLastCalledWith({ selectedValues: [2, 4] });
  });

  it('qayta bosilganda variant bekor qilinadi', async () => {
    const user = userEvent.setup();
    const onAnswer = vi.fn();
    render(<ControlledMultiChoiceQuestion onAnswer={onAnswer} />);

    await user.click(screen.getByRole('checkbox', { name: 'Adabiyot' }));
    await user.click(screen.getByRole('checkbox', { name: 'Adabiyot' }));

    expect(onAnswer).toHaveBeenLastCalledWith({ selectedValues: [] });
  });

  it("minSelections belgilangan bo'lsa kamida N ta tanlash haqida yordamchi matn ko'rsatiladi", () => {
    renderQuestion({ question: { ...BASE_QUESTION, minSelections: 2 } });
    expect(screen.getByText('Kamida 2 ta tanlang.')).toBeInTheDocument();
  });

  it("maxSelections ga yetilganda qolgan variantlar disabled bo'ladi", async () => {
    const user = userEvent.setup();
    const onAnswer = vi.fn();
    render(
      <ControlledMultiChoiceQuestion
        question={{ ...BASE_QUESTION, maxSelections: 2 }}
        onAnswer={onAnswer}
      />,
    );

    await user.click(screen.getByRole('checkbox', { name: 'Matematika' }));
    await user.click(screen.getByRole('checkbox', { name: 'Adabiyot' }));

    expect(screen.getByRole('checkbox', { name: 'Biologiya' })).toBeDisabled();
    expect(screen.getByRole('checkbox', { name: 'Tarix' })).toBeDisabled();
    // Allaqachon tanlangan variantlar disabled EMAS — bekor qilib bo'ladi.
    expect(screen.getByRole('checkbox', { name: 'Matematika' })).not.toBeDisabled();

    await user.click(screen.getByRole('checkbox', { name: 'Biologiya' }));
    expect(onAnswer).not.toHaveBeenCalledWith({ selectedValues: [1, 2, 3] });
  });

  it("Ko'pi bilan N ta yordamchi matni ko'rsatiladi (ixtiyoriy savol — minimal chegara yo'q)", () => {
    renderQuestion({ question: { ...BASE_QUESTION, isRequired: false, maxSelections: 3 } });
    expect(screen.getByText("Ko'pi bilan 3 ta tanlang.")).toBeInTheDocument();
  });

  it("invalid=true va tanlov yetarli bo'lmasa 'Kamida N ta' xato xabari chiqadi", () => {
    renderQuestion({ question: { ...BASE_QUESTION, minSelections: 2 }, invalid: true, value: [] });
    expect(screen.getByRole('alert')).toHaveTextContent('Kamida 2 ta variant tanlang.');
  });

  it("invalid=false bo'lsa xato xabari ko'rinmaydi", () => {
    renderQuestion({ invalid: false });
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('klaviatura bilan Tab orqali barcha checkboxlarga yetib borish mumkin (native tartib)', async () => {
    const user = userEvent.setup();
    renderQuestion();

    await user.tab();
    expect(screen.getByRole('checkbox', { name: 'Matematika' })).toHaveFocus();
    await user.tab();
    expect(screen.getByRole('checkbox', { name: 'Adabiyot' })).toHaveFocus();
  });
});
