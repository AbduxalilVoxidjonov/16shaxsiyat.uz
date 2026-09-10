import { useState } from 'react';
import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { VisibilityRule } from '@/shared/lib/visibility';
import type { VisibilityEditorQuestion } from '../model/visibilityEditorHelpers';
import { VisibilityRuleEditor } from './VisibilityRuleEditor';

const QUESTIONS: VisibilityEditorQuestion[] = [
  {
    code: 'Q1_6',
    textUz: "Hozirda qo'shimcha o'quv kurslariga qatnashasizmi?",
    type: 'SingleChoice',
    order: 6,
    options: [
      { textUz: "Ha, Intellect o'quv markazida o'qiyman", value: 1, displayOrder: 1 },
      { textUz: 'Yo’q', value: 2, displayOrder: 2 },
    ],
  },
  {
    code: 'Q2A_1',
    textUz: "Intellect markazida qaysi fan(lar)dan ta'lim olasiz?",
    type: 'MultiChoice',
    order: 7,
    options: [
      { textUz: 'Ingliz tili', value: 1, displayOrder: 1 },
      { textUz: 'Matematika', value: 2, displayOrder: 2 },
    ],
  },
  {
    code: 'Q1_1',
    textUz: 'F.I.Sh.',
    type: 'ShortText',
    order: 1,
    options: null,
  },
];

function Harness({ initial = null }: { initial?: VisibilityRule | null }) {
  const [value, setValue] = useState<VisibilityRule | null>(initial);
  return <VisibilityRuleEditor value={value} onChange={setValue} availableQuestions={QUESTIONS} />;
}

describe('VisibilityRuleEditor', () => {
  it("manba savol yo'q bo'lsa shart qo'shish tugmasi o'chirilgan", () => {
    render(<VisibilityRuleEditor value={null} onChange={() => {}} availableQuestions={[]} />);
    expect(screen.getByRole('button', { name: /Shart qo'shish/ })).toBeDisabled();
    expect(screen.getByText(/oldinroq turgan savol yo'q/)).toBeInTheDocument();
  });

  it("'Shart qo'shish' bosilganda standart shart yaratiladi va jonli ko'rinish chiqadi", async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.click(screen.getByRole('button', { name: /Shart qo'shish/ }));

    // Standart shart — birinchi savol (Q1_6, SingleChoice) + `Equals` (operator ro'yxatida birinchi).
    expect(screen.getByText(/Jonli oldindan ko'rish/)).toBeInTheDocument();
    expect(
      screen.getByText(/Q1_6 savoliga javob "Ha, Intellect o'quv markazida o'qiyman" bo'lsa/),
    ).toBeInTheDocument();
  });

  it('manba savolni matn turiga almashtirsa operator ro‘yxati Answered/NotAnswered ga qisqaradi va qiymat maydoni yo‘qoladi', async () => {
    const user = userEvent.setup();
    render(<Harness initial={{ match: 'All', conditions: [{ questionCode: 'Q1_6', operator: 'Equals', values: [1] }] }} />);

    await user.selectOptions(screen.getByLabelText('Manba savol'), 'Q1_1');

    const operatorSelect = screen.getByLabelText('Operator') as HTMLSelectElement;
    const optionValues = [...operatorSelect.options].map((o) => o.value);
    expect(optionValues).toEqual(['Answered', 'NotAnswered']);
    expect(screen.queryByText('Qiymat')).not.toBeInTheDocument();
  });

  it('MultiChoice savolda ContainsAny tanlansa checkbox variantlar ko‘rinadi va bir nechtasini tanlash mumkin', async () => {
    const user = userEvent.setup();
    render(
      <Harness
        initial={{ match: 'All', conditions: [{ questionCode: 'Q2A_1', operator: 'ContainsAny', values: [1] }] }}
      />,
    );

    const inglizCheckbox = screen.getByRole('checkbox', { name: 'Ingliz tili' });
    const matematikaCheckbox = screen.getByRole('checkbox', { name: 'Matematika' });
    expect(inglizCheckbox).toBeChecked();
    expect(matematikaCheckbox).not.toBeChecked();

    await user.click(matematikaCheckbox);
    expect(matematikaCheckbox).toBeChecked();
    expect(inglizCheckbox).toBeChecked();
  });

  it("shartni o'chirish qoidani butunlay olib tashlaydi (bitta shart qolganda)", async () => {
    const user = userEvent.setup();
    render(
      <Harness
        initial={{ match: 'All', conditions: [{ questionCode: 'Q1_6', operator: 'Equals', values: [1] }] }}
      />,
    );

    await user.click(screen.getByRole('button', { name: /1-shartni o'chirish/ }));
    expect(screen.getByRole('button', { name: /Shart qo'shish/ })).toBeInTheDocument();
  });

  it("'Shartni olib tashlash' butun qoidani null qiladi", async () => {
    const user = userEvent.setup();
    render(
      <Harness
        initial={{ match: 'All', conditions: [{ questionCode: 'Q1_6', operator: 'Equals', values: [1] }] }}
      />,
    );

    await user.click(screen.getByRole('button', { name: 'Shartni olib tashlash' }));
    expect(screen.getByRole('button', { name: /Shart qo'shish/ })).toBeInTheDocument();
  });

  it("ikkitadan ortiq shart bo'lganda VA/YOKI tanlovi ko'rinadi", async () => {
    const user = userEvent.setup();
    render(
      <Harness
        initial={{
          match: 'All',
          conditions: [
            { questionCode: 'Q1_6', operator: 'Equals', values: [1] },
            { questionCode: 'Q1_1', operator: 'Answered', values: [] },
          ],
        }}
      />,
    );

    expect(screen.getByLabelText('Shartlarni qanday birlashtirish')).toBeInTheDocument();
    await user.selectOptions(screen.getByLabelText('Shartlarni qanday birlashtirish'), 'Any');
    expect(screen.getByText(/ YOKI /)).toBeInTheDocument();
  });
});
