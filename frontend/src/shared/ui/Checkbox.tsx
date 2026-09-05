import { forwardRef, useId, type InputHTMLAttributes, type ReactNode } from 'react';
import { cn } from '@/shared/lib/cn';

export interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label: ReactNode;
  /** Maydon ostida ko'rsatiladigan xato matni; berilsa `aria-invalid` avtomatik qo'yiladi. */
  error?: string;
}

/**
 * Yorliqli, xato matnli bazaviy checkbox — masalan rozilik bloki (`docs/11` E-2).
 * Butun yorliq bosiladigan qiladi va sensor nishon balandligi ≥ 44px (`docs/11`, 4-bo'lim).
 */
export const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(function Checkbox(
  { className, label, error, id, ...props },
  ref,
) {
  const generatedId = useId();
  const inputId = id ?? generatedId;
  const descriptionId = error ? `${inputId}-error` : undefined;

  return (
    <div className="flex flex-col gap-1.5">
      <label
        htmlFor={inputId}
        className={cn('flex min-h-11 cursor-pointer items-start gap-3 py-1', className)}
      >
        <input
          ref={ref}
          id={inputId}
          type="checkbox"
          className={cn(
            'mt-0.5 size-5 shrink-0 rounded-md border-ink-faint text-firuza-600',
            'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-firuza-600',
            error && 'border-terakota-600',
          )}
          aria-invalid={Boolean(error) || undefined}
          aria-describedby={descriptionId}
          {...props}
        />
        <span className="text-sm text-ink-soft">{label}</span>
      </label>
      {error && (
        <p id={descriptionId} className="text-sm text-terakota-700" role="alert">
          {error}
        </p>
      )}
    </div>
  );
});
