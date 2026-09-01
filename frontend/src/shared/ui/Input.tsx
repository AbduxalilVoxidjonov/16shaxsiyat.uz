import { forwardRef, useId, type InputHTMLAttributes } from 'react';
import { cn } from '@/shared/lib/cn';

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  /** Maydon ostida ko'rsatiladigan xato matni; berilsa `aria-invalid` avtomatik qo'yiladi. */
  error?: string;
  /** Yordamchi matn (xato yo'q holatda). */
  hint?: string;
}

/** Yorliqli, xato/yordamchi matnli bazaviy input — barcha formalar shu komponentdan foydalanadi. */
export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { className, label, error, hint, id, ...props },
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
      <input
        ref={ref}
        id={inputId}
        className={cn(
          'h-11 rounded-lg border border-neutral-300 bg-white px-3 text-base text-neutral-900',
          'placeholder:text-neutral-400',
          'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-600',
          'disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-neutral-400',
          error && 'border-danger-500',
          className,
        )}
        aria-invalid={Boolean(error) || undefined}
        aria-describedby={descriptionId}
        {...props}
      />
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
