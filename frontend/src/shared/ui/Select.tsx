import { forwardRef, useId, type SelectHTMLAttributes } from 'react';
import { ChevronDown } from 'lucide-react';
import { cn } from '@/shared/lib/cn';

export interface SelectOption {
  value: string;
  label: string;
  disabled?: boolean;
}

export interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: string;
  error?: string;
  hint?: string;
  options: SelectOption[];
  /** Bo'sh tanlov uchun (masalan, "Tanlang…"). i18n orqali beriladi, hardcode qilinmaydi. */
  placeholder?: string;
}

/** Yorliqli, native `<select>` asosidagi bazaviy tanlov komponenti. */
export const Select = forwardRef<HTMLSelectElement, SelectProps>(function Select(
  { className, label, error, hint, options, placeholder, id, ...props },
  ref,
) {
  const generatedId = useId();
  const selectId = id ?? generatedId;
  const descriptionId = error ? `${selectId}-error` : hint ? `${selectId}-hint` : undefined;

  return (
    <div className="flex flex-col gap-1.5">
      {label && (
        <label htmlFor={selectId} className="text-sm font-medium text-neutral-700">
          {label}
        </label>
      )}
      <div className="relative">
        <select
          ref={ref}
          id={selectId}
          className={cn(
            'h-11 w-full appearance-none rounded-lg border border-neutral-300 bg-white px-3 pr-9 text-base text-neutral-900',
            'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-600',
            'disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-400',
            error && 'border-danger-500',
            className,
          )}
          aria-invalid={Boolean(error) || undefined}
          aria-describedby={descriptionId}
          {...props}
        >
          {placeholder && (
            <option value="" disabled hidden>
              {placeholder}
            </option>
          )}
          {options.map((option) => (
            <option key={option.value} value={option.value} disabled={option.disabled}>
              {option.label}
            </option>
          ))}
        </select>
        <ChevronDown
          className="pointer-events-none absolute top-1/2 right-3 size-4 -translate-y-1/2 text-neutral-500"
          aria-hidden="true"
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
