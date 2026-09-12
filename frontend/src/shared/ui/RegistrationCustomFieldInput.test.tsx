import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useForm } from 'react-hook-form';
import type { RegistrationFormCustomField } from '@/shared/api/registrationFormSettingsTypes';
import { RegistrationCustomFieldInput } from './RegistrationCustomFieldInput';

/**
 * `RegistrationCustomFieldInput` — `shared/ui`, `features/public-assessment` VA
 * `features/public-account` ikkalasi ham ishlatadi (`docs/10` §2: features bir-birini
 * import qilmaydi). Bu yerda haqiqiy `useForm` bilan sinaladi — Controller generik
 * `Path<TFieldValues>`/`never` kastlari (`RegistrationCustomFieldInput.tsx`dagi izohga
 * qarang) runtime'da ishlashini tasdiqlaydi.
 */
interface FormValues {
  customFields: Record<string, string | string[]>;
}

function Harness({
  field,
  onSubmit,
  error,
}: {
  field: RegistrationFormCustomField;
  onSubmit: (values: FormValues) => void;
  error?: string;
}) {
  const { control, handleSubmit } = useForm<FormValues>({
    defaultValues: { customFields: { [field.code]: field.type === 'MultiChoice' ? [] : '' } },
  });

  return (
    <form onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
      <RegistrationCustomFieldInput
        field={field}
        control={control}
        name={`customFields.${field.code}`}
        error={error}
      />
      <button type="submit">Yuborish</button>
    </form>
  );
}

const SHORT_TEXT: RegistrationFormCustomField = {
  code: 'PARENT_JOB',
  type: 'ShortText',
  labelUz: 'Ota-onangiz kasbi',
  placeholderUz: "Masalan: o'qituvchi",
  requirement: 'Optional',
  maxLength: 200,
  inputPattern: null,
  options: null,
  order: 9,
};

const LONG_TEXT: RegistrationFormCustomField = {
  ...SHORT_TEXT,
  code: 'ABOUT',
  type: 'LongText',
  labelUz: "O'zingiz haqingizda",
};

const PHONE: RegistrationFormCustomField = {
  ...SHORT_TEXT,
  code: 'WORK_PHONE',
  type: 'Phone',
  labelUz: 'Ish telefoni',
};

const SINGLE_CHOICE: RegistrationFormCustomField = {
  code: 'TRANSPORT',
  type: 'SingleChoice',
  labelUz: 'Maktabga qanday borasiz?',
  placeholderUz: null,
  requirement: 'Optional',
  maxLength: null,
  inputPattern: null,
  options: [
    { textUz: 'Piyoda', value: 'foot', order: 1 },
    { textUz: 'Avtobus', value: 'bus', order: 2 },
  ],
  order: 10,
};

const MULTI_CHOICE: RegistrationFormCustomField = {
  ...SINGLE_CHOICE,
  code: 'HOBBIES',
  type: 'MultiChoice',
  labelUz: 'Qiziqishlar',
  options: [
    { textUz: 'Sport', value: 'sport', order: 1 },
    { textUz: "San'at", value: 'art', order: 2 },
  ],
};

describe('RegistrationCustomFieldInput', () => {
  it('ShortText — yozilgan matn submit qiymatiga tushadi', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(<Harness field={SHORT_TEXT} onSubmit={onSubmit} />);

    await user.type(screen.getByLabelText('Ota-onangiz kasbi'), "O'qituvchi");
    expect(screen.getByPlaceholderText("Masalan: o'qituvchi")).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Yuborish' }));

    expect(onSubmit).toHaveBeenCalledWith(
      { customFields: { PARENT_JOB: "O'qituvchi" } },
      expect.anything(),
    );
  });

  it('LongText — Textarea sifatida chiziladi', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(<Harness field={LONG_TEXT} onSubmit={onSubmit} />);

    const textarea = screen.getByLabelText("O'zingiz haqingizda");
    expect(textarea.tagName).toBe('TEXTAREA');
    await user.type(textarea, 'Salom');
    await user.click(screen.getByRole('button', { name: 'Yuborish' }));

    expect(onSubmit).toHaveBeenCalledWith({ customFields: { ABOUT: 'Salom' } }, expect.anything());
  });

  it("Phone — oddiy matn maydoni sifatida (type=tel, +998 formatlashsiz)", async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(<Harness field={PHONE} onSubmit={onSubmit} />);

    const input = screen.getByLabelText('Ish telefoni');
    expect(input).toHaveAttribute('type', 'tel');
    await user.type(input, '901234567');
    await user.click(screen.getByRole('button', { name: 'Yuborish' }));

    expect(onSubmit).toHaveBeenCalledWith(
      { customFields: { WORK_PHONE: '901234567' } },
      expect.anything(),
    );
  });

  it("SingleChoice — variant tanlanganda submit qiymati variant order'i (butun son emas, string)", async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(<Harness field={SINGLE_CHOICE} onSubmit={onSubmit} />);

    await user.click(screen.getByText('Avtobus'));
    await user.click(screen.getByRole('button', { name: 'Yuborish' }));

    expect(onSubmit).toHaveBeenCalledWith({ customFields: { TRANSPORT: '2' } }, expect.anything());
  });

  it('MultiChoice — bir nechta variant tanlash/bekor qilish massivga qo\'shadi/olib tashlaydi', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(<Harness field={MULTI_CHOICE} onSubmit={onSubmit} />);

    await user.click(screen.getByLabelText('Sport'));
    await user.click(screen.getByLabelText("San'at"));
    await user.click(screen.getByRole('button', { name: 'Yuborish' }));
    expect(onSubmit).toHaveBeenLastCalledWith(
      { customFields: { HOBBIES: ['1', '2'] } },
      expect.anything(),
    );

    onSubmit.mockClear();
    await user.click(screen.getByLabelText('Sport')); // bekor qilish
    await user.click(screen.getByRole('button', { name: 'Yuborish' }));
    expect(onSubmit).toHaveBeenLastCalledWith(
      { customFields: { HOBBIES: ['2'] } },
      expect.anything(),
    );
  });

  it('ShortText/Phone/LongText uchun error propi Input/Textarea ostida ko\'rsatiladi', () => {
    render(
      <Harness field={SHORT_TEXT} error="Bu maydon kiritilishi shart." onSubmit={() => {}} />,
    );
    expect(screen.getByRole('alert')).toHaveTextContent('Bu maydon kiritilishi shart.');
  });

  it("SingleChoice/MultiChoice uchun error propi fieldset ostida role=\"alert\" bilan ko'rsatiladi", () => {
    render(
      <Harness field={SINGLE_CHOICE} error="Variantni tanlang." onSubmit={() => {}} />,
    );
    expect(screen.getByRole('alert')).toHaveTextContent('Variantni tanlang.');
  });
});
