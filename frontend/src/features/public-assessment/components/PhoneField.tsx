import { forwardRef, useId, type InputHTMLAttributes } from 'react';
import { cn } from '@/shared/lib/cn';
import { extractUzLocalDigits, formatUzLocalDigits } from '@/shared/lib/formatPhone';

export interface PhoneFieldProps
  extends Omit<InputHTMLAttributes<HTMLInputElement>, 'value' | 'onChange' | 'type'> {
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
        <label htmlFor={inputId} className="text-sm font-medium text-neutral-700">
          {label}
        </label>
      )}
      <div
        className={cn(
          'flex h-11 items-center rounded-lg border border-neutral-300 bg-white pl-3',
          'focus-within:outline focus-within:outline-2 focus-within:outline-offset-2 focus-within:outline-primary-600',
          error && 'border-danger-500',
        )}
      >
        <span className="shrink-0 text-base text-neutral-500 select-none" aria-hidden="true">
          +998
        </span>
        <input
          ref={ref}
          id={inputId}
          type="tel"
          inputMode="numeric"
          placeholder="(__) ___-__-__"
          className={cn(
            'h-full flex-1 rounded-r-lg bg-transparent px-2 text-base text-neutral-900',
            'placeholder:text-neutral-400 focus:outline-none',
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
        <p id={descriptionId} className="text-sm text-danger-600" role="alert">
          {error}
        </p>
      )}
      {!error && hint && (
        <p id={descriptionId} className="text-sm text-neutral-500">
          {hint}
        </p>
      )}
    </div>
  );
});
