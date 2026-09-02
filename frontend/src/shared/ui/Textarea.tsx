import { forwardRef, useId, type TextareaHTMLAttributes } from 'react';
import { cn } from '@/shared/lib/cn';

export interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string;
  error?: string;
  hint?: string;
}

/** `Input.tsx` bilan bir xil naqsh, ko'p qatorli matn uchun (masalan maktab izohi). */
export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(function Textarea(
  { className, label, error, hint, id, rows = 3, ...props },
  ref,
) {
  const generatedId = useId();
  const textareaId = id ?? generatedId;
  const descriptionId = error ? `${textareaId}-error` : hint ? `${textareaId}-hint` : undefined;

  return (
    <div className="flex flex-col gap-1.5">
      {label && (
        <label htmlFor={textareaId} className="text-sm font-medium text-neutral-700">
          {label}
        </label>
      )}
      <textarea
        ref={ref}
        id={textareaId}
        rows={rows}
        className={cn(
          'rounded-lg border border-neutral-300 bg-white px-3 py-2 text-base text-neutral-900',
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
