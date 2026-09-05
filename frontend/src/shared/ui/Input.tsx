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
        <label htmlFor={inputId} className="text-sm font-medium text-ink-soft">
          {label}
        </label>
      )}
      <input
        ref={ref}
        id={inputId}
        className={cn(
          // Chegara `ink-faint` (#A79C91): `line-strong` (#DCD0BE) oq fonda atigi 1.52:1
          // beradi va maydon chekkasi ko'rinmay qoladi. `ink-faint` 2.69:1 — WCAG 1.4.11
          // (3:1) ga hali yetmaydi, lekin eski `neutral-300` (1.47:1) dan sezilarli yaxshi.
          'h-11 rounded-2xl border border-ink-faint bg-paper-card px-3 text-base text-ink',
          'placeholder:text-ink-muted',
          'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-firuza-600',
          'disabled:cursor-not-allowed disabled:bg-paper-deep disabled:text-ink-faint',
          error && 'border-terakota-600',
          className,
        )}
        aria-invalid={Boolean(error) || undefined}
        aria-describedby={descriptionId}
        {...props}
      />
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
