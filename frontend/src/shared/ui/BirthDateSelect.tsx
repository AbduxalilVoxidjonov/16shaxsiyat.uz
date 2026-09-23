import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import type { BirthDateValue } from '@/shared/lib/birthDate';
import { RequiredMark } from './RequiredMark';
import { Select, type SelectOption } from './Select';

export type { BirthDateValue };

export interface BirthDateSelectProps {
  value: BirthDateValue;
  onChange: (value: BirthDateValue) => void;
  onBlur?: () => void;
  errors?: { day?: string; month?: string; year?: string };
  minAge: number;
  maxAge: number;
  /**
   * Superadmin "Sozlamalar"da maydon yorlig'ini o'zgartirgan bo'lishi mumkin (P52 2-to'lqin,
   * `docs/18` §9.6.2) — berilmasa standart i18n matni (`register.fields.birthDate`) ishlatiladi.
   */
  label?: string;
  /** Majburiy: legend oxirida qizil `*` va har bir select'da `aria-required`. */
  isRequired?: boolean;
}

const MONTH_KEYS = [
  'jan',
  'feb',
  'mar',
  'apr',
  'may',
  'jun',
  'jul',
  'aug',
  'sep',
  'oct',
  'nov',
  'dec',
] as const;

/**
 * Tug'ilgan sana — 3 ta native select (kun/oy/yil), kalendar emas (telefonda qulayroq,
 * `docs/11` E-2). Yil oralig'i `minAge`/`maxAge` atrofida generatsiya qilinadi; aniq yosh
 * tekshiruvi (`registrationSchema.ts`) alohida, bu yerdagi oraliq faqat tanlov ro'yxatini
 * qisqartirish uchun (biroz keng — chegara holatlari uchun).
 */
export function BirthDateSelect({
  value,
  onChange,
  onBlur,
  errors,
  minAge,
  maxAge,
  label,
  isRequired,
}: BirthDateSelectProps) {
  const { t } = useTranslation();

  const dayOptions = useMemo<SelectOption[]>(
    () =>
      Array.from({ length: 31 }, (_, index) => {
        const day = String(index + 1);
        return { value: day, label: day.padStart(2, '0') };
      }),
    [],
  );

  const monthOptions = useMemo<SelectOption[]>(
    () =>
      MONTH_KEYS.map((key, index) => ({
        value: String(index + 1),
        label: t(`register.months.${key}`),
      })),
    [t],
  );

  const yearOptions = useMemo<SelectOption[]>(() => {
    const currentYear = new Date().getFullYear();
    const from = currentYear - maxAge - 1;
    const to = currentYear - minAge;
    const years: SelectOption[] = [];
    for (let year = to; year >= from; year -= 1) {
      years.push({ value: String(year), label: String(year) });
    }
    return years;
  }, [minAge, maxAge]);

  return (
    <fieldset className="flex flex-col gap-1.5">
      <legend className="mb-1.5 text-sm font-medium text-ink-soft">
        {label || t('register.fields.birthDate')}
        {isRequired && <RequiredMark />}
      </legend>
      <div className="grid grid-cols-3 gap-2">
        <Select
          isRequired={isRequired}
          aria-label={t('register.fields.birthDay')}
          placeholder={t('register.fields.birthDayPlaceholder')}
          options={dayOptions}
          value={value.day}
          error={errors?.day}
          onChange={(event) => {
            onChange({ ...value, day: event.target.value });
          }}
          onBlur={onBlur}
        />
        <Select
          isRequired={isRequired}
          aria-label={t('register.fields.birthMonth')}
          placeholder={t('register.fields.birthMonthPlaceholder')}
          options={monthOptions}
          value={value.month}
          error={errors?.month}
          onChange={(event) => {
            onChange({ ...value, month: event.target.value });
          }}
          onBlur={onBlur}
        />
        <Select
          isRequired={isRequired}
          aria-label={t('register.fields.birthYear')}
          placeholder={t('register.fields.birthYearPlaceholder')}
          options={yearOptions}
          value={value.year}
          error={errors?.year}
          onChange={(event) => {
            onChange({ ...value, year: event.target.value });
          }}
          onBlur={onBlur}
        />
      </div>
    </fieldset>
  );
}
