import { forwardRef, useId, type InputHTMLAttributes } from 'react';
import { cn } from '@/shared/lib/cn';
import { extractUzLocalDigits, formatUzLocalDigits } from '@/shared/lib/formatPhone';

export interface PhoneFieldProps extends Omit<
  InputHTMLAttributes<HTMLInputElement>,
  'value' | 'onChange' | 'type'
> {
  label?: string;
  error?: string;
  hint?: string;
  /** Mahalliy raqamlar (ko'pi bilan 9 ta), `+998`siz — E.164'ga o'girish chaqiruvchi tomonda. */
  value: string;
  onValueChange: (digits: string) => void;
}

/**
 * `+998 (__) ___-__-__` maskali telefon maydoni (`docs/11` E-2, `prompts/20`).
 * `+998` prefiksi tahrirlanmaydigan belgi sifatida ko'rsatiladi — faqat mahalliy 9 ta raqam
 * tahrirlanadi, bu kursor/o'chirish xatti-harakatini oddiy va bashorat qilinadigan qiladi.
 *
 * Maydon chromi (`h-11 rounded-2xl border-ink-faint bg-paper-card`, xato — `terakota-600`,
 * fokus — `firuza-600`) ATAYLAB `Input` bilan BIR XIL yozilgan: bu komponent
 * `<input>`ni `+998` prefiksi bilan o'rab turgani uchun `Input`ni qayta ishlata olmaydi,
 * lekin anketada ikkalasi yonma-yon turadi — chegara radiusi yoki xato rangi farq qilsa
 * darhol ko'zga tashlanadi. `Input` uslubi o'zgarsa, bu yer ham yangilanishi kerak.
 */
export const PhoneField = forwardRef<HTMLInputElement, PhoneFieldProps>(function PhoneField(
  { className, label, error, hint, value, onValueChange, id, ...props },
  ref,
) {
  const generatedId = useId();
  const inputId = id ?? generatedId;
  const descriptionId = error ? `${inputId}-error` : hint ? `${inputId}-hint` : undefined;

  return (
    <div className="flex flex-col gap-1.5">
      {label && (
        <label htmlFor={inputId} className="text-sm font-medium text-ink-soft">
          {label}
        </label>
      )}
      <div
        className={cn(
          'flex h-11 items-center rounded-2xl border bg-paper-card pl-3',
          'focus-within:outline focus-within:outline-2 focus-within:outline-offset-2 focus-within:outline-firuza-600',
          error ? 'border-terakota-600' : 'border-ink-faint',
        )}
      >
        <span className="shrink-0 text-base text-ink-muted select-none" aria-hidden="true">
          +998
        </span>
        <input
          ref={ref}
          id={inputId}
          type="tel"
          inputMode="numeric"
          placeholder="(__) ___-__-__"
          className={cn(
            'h-full flex-1 rounded-r-2xl bg-transparent px-2 text-base text-ink',
            'placeholder:text-ink-muted focus:outline-none',
            className,
          )}
          value={formatUzLocalDigits(value)}
          onChange={(event) => {
            onValueChange(extractUzLocalDigits(event.target.value));
          }}
          aria-invalid={Boolean(error) || undefined}
          aria-describedby={descriptionId}
          {...props}
        />
      </div>
      {error && (
        <p id={descriptionId} className="text-sm text-terakota-700" role="alert">
          {error}
        </p>
      )}
      {!error && hint && (
        <p id={descriptionId} className="text-sm text-ink-soft">
          {hint}
        </p>
      )}
    </div>
  );
});
