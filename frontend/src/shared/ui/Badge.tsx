import type { HTMLAttributes } from 'react';
import { cn } from '@/shared/lib/cn';

export type BadgeVariant = 'primary' | 'success' | 'warning' | 'danger' | 'neutral';

export interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  variant?: BadgeVariant;
}

// Har bir juftlik `text-xs` (kichik matn) uchun WCAG AA 4.5:1 dan yuqori:
// primary 7.22:1 · success 7.85:1 · warning 6.91:1 · danger 8.88:1 · neutral 8.59:1.
const VARIANT_CLASSES: Record<BadgeVariant, string> = {
  primary: 'bg-firuza-100 text-firuza-800',
  success: 'bg-zumrad-100 text-zumrad-800',
  warning: 'bg-zarhal-100 text-zarhal-800',
  danger: 'bg-terakota-100 text-terakota-800',
  neutral: 'bg-paper-deep text-ink-soft',
};

/**
 * Holat yorlig'i — masalan `Reliable`, `Analyzing`, `AnalysisFailed`.
 * Rang faqat semantik holatni bildiradi, ball darajasini emas (docs/11, 1-bo'lim).
 */
export function Badge({ className, variant = 'neutral', ...props }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
        VARIANT_CLASSES[variant],
        className,
      )}
      {...props}
    />
  );
}
