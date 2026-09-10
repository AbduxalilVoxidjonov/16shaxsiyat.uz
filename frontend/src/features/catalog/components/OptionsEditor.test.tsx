import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { QuestionOptionFormValue } from '../model/questionOptions';
import { OptionsEditor } from './OptionsEditor';

function options(): QuestionOptionFormValue[] {
  return [
    { textUz: 'A', value: 1, displayOrder: 1 },
    { textUz: 'B', value: 2, displayOrder: 2 },
  ];
}

describe('OptionsEditor', () => {
  it("bo'sh bo'lsa yordam matni ko'rsatadi", () => {
    render(<OptionsEditor options={[]} onChange={vi.fn()} />);
    expect(screen.getByText(/Hali variant/)).toBeInTheDocument();
  });

  it("'Variant qo'shish' bosilganda yangi variant qo'shiladi (keyingi value/order bilan)", async () => {
    const onChange = vi.fn();
    const user = userEvent.setup();
    render(<OptionsEditor options={options()} onChange={onChange} />);

    await user.click(screen.getByRole('button', { name: /Variant qo'shish/ }));

    expect(onChange).toHaveBeenCalledWith([
      { textUz: 'A', value: 1, displayOrder: 1 },
      { textUz: 'B', value: 2, displayOrder: 2 },
      { textUz: '', value: 3, displayOrder: 3 },
    ]);
  });

  it("o'chirish tugmasi bosilganda variant olib tashlanadi va qolganlar qayta tartiblanadi", async () => {
    const onChange = vi.fn();
    const user = userEvent.setup();
    render(<OptionsEditor options={options()} onChange={onChange} />);

    await user.click(screen.getByRole('button', { name: /1-variantni o'chirish/ }));

    expect(onChange).toHaveBeenCalledWith([{ textUz: 'B', value: 2, displayOrder: 1 }]);
  });

  it('takroriy qiymat ogohlantiradi', () => {
    render(
      <OptionsEditor
        options={[
          { textUz: 'A', value: 1, displayOrder: 1 },
          { textUz: 'B', value: 1, displayOrder: 2 },
        ]}
        onChange={vi.fn()}
      />,
    );

    expect(screen.getByRole('alert')).toHaveTextContent('1');
  });

  it('matnni tahrirlash onChange chaqiradi', async () => {
    const onChange = vi.fn();
    const user = userEvent.setup();
    render(<OptionsEditor options={options()} onChange={onChange} />);

    const firstText = screen.getByLabelText('Variant matni');
    await user.type(firstText, '!');

    expect(onChange).toHaveBeenCalled();
  });
});
