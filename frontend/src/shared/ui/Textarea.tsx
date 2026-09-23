import { forwardRef, useId, type TextareaHTMLAttributes } from 'react';
import { cn } from '@/shared/lib/cn';
import { RequiredMark } from './RequiredMark';

export interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string;
  /**
   * Majburiy maydon: yorliq oxirida qizil `*` (`RequiredMark`, `aria-hidden`) va maydonda
   * `aria-required`. Native `required` EMAS — formalar `noValidate`, tekshiruv zod'da.
   */
  isRequired?: boolean;
  error?: string;
  hint?: string;
}

/** `Input.tsx` bilan bir xil naqsh, ko'p qatorli matn uchun (masalan maktab izohi). */
export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(function Textarea(
  { className, label, isRequired, error, hint, id, rows = 3, ...props },
  ref,
) {
  const generatedId = useId();
  const textareaId = id ?? generatedId;
  const descriptionId = error ? `${textareaId}-error` : hint ? `${textareaId}-hint` : undefined;

  return (
    <div className="flex flex-col gap-1.5">
      {label && (
        <label htmlFor={textareaId} className="text-sm font-medium text-ink-soft">
          {label}
          {isRequired && <RequiredMark />}
        </label>
      )}
      <textarea
        ref={ref}
        id={textareaId}
        rows={rows}
        className={cn(
          // Chegara rangi haqida `Input.tsx` dagi izohga qarang (`ink-faint`, 2.69:1).
          'rounded-2xl border border-ink-faint bg-paper-card px-3 py-2 text-base text-ink',
          'placeholder:text-ink-muted',
          'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-firuza-600',
          'disabled:cursor-not-allowed disabled:bg-paper-deep disabled:text-ink-faint',
          error && 'border-terakota-600',
          className,
        )}
        aria-required={isRequired || undefined}
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
