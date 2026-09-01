import type { HTMLAttributes } from 'react';
import { cn } from '@/shared/lib/cn';

export type BadgeVariant = 'primary' | 'success' | 'warning' | 'danger' | 'neutral';

export interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  variant?: BadgeVariant;
}

const VARIANT_CLASSES: Record<BadgeVariant, string> = {
  primary: 'bg-primary-100 text-primary-700',
  success: 'bg-success-100 text-success-700',
  warning: 'bg-warning-100 text-warning-700',
  danger: 'bg-danger-100 text-danger-700',
  neutral: 'bg-neutral-100 text-neutral-700',
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
