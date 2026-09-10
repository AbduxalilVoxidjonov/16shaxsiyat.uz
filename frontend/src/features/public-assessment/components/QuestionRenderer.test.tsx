import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QuestionRenderer } from './QuestionRenderer';
import type { BranchingQuestion } from '@/shared/api/branchingTypes';
import type { PublicScaleLabel } from '@/shared/api/types';

const SCALE_LABELS: PublicScaleLabel[] = [
  { value: 1, label: "Umuman qo'shilmayman" },
  { value: 5, label: "To'liq qo'shilaman" },
];

const BASE_QUESTION: BranchingQuestion = {
  id: 'q1',
  code: 'Q1',
  order: 1,
  text: 'Savol matni',
  type: 'Likert5',
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

function renderRenderer(question: BranchingQuestion, overrides: Partial<Parameters<typeof QuestionRenderer>[0]> = {}) {
  const onAnswer = vi.fn();
  const onAdvance = vi.fn();
  render(
    <QuestionRenderer
      question={question}
      scaleLabels={SCALE_LABELS}
      currentValue={null}
      currentText={null}
      currentSelectedValues={[]}
      invalid={false}
      onAnswer={onAnswer}
      onAdvance={onAdvance}
      {...overrides}
    />,
  );
  return { onAnswer, onAdvance };
}

describe('QuestionRenderer', () => {
  it('Likert5 uchun LikertQuestion (radiogroup) ko\'rsatiladi va onAnswer {value} bilan chaqiriladi', async () => {
    const user = userEvent.setup();
    const { onAnswer } = renderRenderer({ ...BASE_QUESTION, type: 'Likert5' });

    expect(screen.getByRole('radiogroup')).toBeInTheDocument();
    await user.click(screen.getByRole('radio', { name: "To'liq qo'shilaman" }));

    expect(onAnswer).toHaveBeenCalledWith({ value: 5 });
  });

  it('ShortText uchun TextQuestion ko\'rsatiladi va onAnswer {text} bilan chaqiriladi', async () => {
    const user = userEvent.setup();
    const { onAnswer } = renderRenderer({ ...BASE_QUESTION, type: 'ShortText' });

    await user.type(screen.getByLabelText(/Savol matni/), 'A');

    expect(onAnswer).toHaveBeenLastCalledWith({ text: 'A' });
  });

  it('Phone uchun ham TextQuestion ko\'rsatiladi (inputMode=tel)', () => {
    renderRenderer({ ...BASE_QUESTION, type: 'Phone' });
    expect(screen.getByLabelText(/Savol matni/)).toHaveAttribute('inputmode', 'tel');
  });

  it('LongText uchun LongTextQuestion (textarea) ko\'rsatiladi va onAnswer {text} bilan chaqiriladi', async () => {
    const user = userEvent.setup();
    const { onAnswer } = renderRenderer({ ...BASE_QUESTION, type: 'LongText' });

    const textarea = screen.getByLabelText(/Savol matni/);
    expect(textarea.tagName).toBe('TEXTAREA');
    await user.type(textarea, 'B');

    expect(onAnswer).toHaveBeenLastCalledWith({ text: 'B' });
  });

  it('MultiChoice uchun MultiChoiceQuestion ko\'rsatiladi va onAnswer {selectedValues} bilan chaqiriladi', async () => {
    const user = userEvent.setup();
    const { onAnswer } = renderRenderer({
      ...BASE_QUESTION,
      type: 'MultiChoice',
      options: [
        { id: 'o1', text: 'Variant A', value: 1, order: 1 },
        { id: 'o2', text: 'Variant B', value: 2, order: 2 },
      ],
    });

    await user.click(screen.getByRole('checkbox', { name: 'Variant A' }));

    expect(onAnswer).toHaveBeenCalledWith({ selectedValues: [1] });
  });
});
