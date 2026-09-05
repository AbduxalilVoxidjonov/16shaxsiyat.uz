import { cn } from '@/shared/lib/cn';

export type PatternBackdropVariant = 'default' | 'light';

export interface PatternBackdropProps {
  className?: string;
  /**
   * `default` — och fon uchun qumrang chiziqli naqsh;
   * `light` — to'q fon uchun yarim shaffof oq chiziqli naqsh.
   */
  variant?: PatternBackdropVariant;
}

const VARIANT_CLASSES: Record<PatternBackdropVariant, string> = {
  default: 'bg-girih',
  light: 'bg-girih-light',
};

/**
 * Dekorativ girih naqshli fon qatlami — pastga qarab so'nadi (`mask-fade-b`).
 *
 * Absolyut joylashadi, shu sabab ota element `relative` (va odatda `overflow-hidden`)
 * bo'lishi kerak. Zichlikni chaqiruv joyida `opacity-*` bilan sozlang.
 */
export function PatternBackdrop({ className, variant = 'default' }: PatternBackdropProps) {
  return (
    <div
      aria-hidden="true"
      className={cn(
        'pointer-events-none absolute inset-0 mask-fade-b',
        VARIANT_CLASSES[variant],
        className,
      )}
    />
  );
}
