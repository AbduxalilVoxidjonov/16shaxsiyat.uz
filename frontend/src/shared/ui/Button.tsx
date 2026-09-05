import { forwardRef, type ButtonHTMLAttributes } from 'react';
import { Loader2 } from 'lucide-react';
import { cn } from '@/shared/lib/cn';

export type ButtonVariant = 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'lg';

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  /** `true` bo'lsa tugma o'chiriladi va yuklanish indikatori ko'rsatiladi. */
  isLoading?: boolean;
}

/*
 * KONTRAST QARORI (P45): `index.css` dagi `.btn-primary` kompozitsiya klassi `firuza-500`
 * (#0F9E9E) ustida oq matn ishlatadi — bu 3.28:1, WCAG AA (4.5:1) dan PAST. Shu sabab bu
 * yerda bir pog'ona to'qroq `firuza-600` (#0B8080) olindi: oq matn bilan 4.76:1 → AA o'tadi.
 * `danger` uchun ham shunday: `terakota-600` (#A44325) + oq = 6.16:1.
 */
const VARIANT_CLASSES: Record<ButtonVariant, string> = {
  primary: 'bg-firuza-600 text-white shadow-soft hover:bg-firuza-700',
  secondary: 'bg-paper-deep text-ink hover:bg-line',
  outline: 'border border-line-strong bg-paper-card text-ink hover:bg-paper-deep',
  ghost: 'text-ink-soft hover:bg-paper-deep hover:text-ink',
  danger:
    'bg-terakota-600 text-white shadow-soft hover:bg-terakota-700 focus-visible:outline-terakota-600',
};

const SIZE_CLASSES: Record<ButtonSize, string> = {
  sm: 'h-9 px-4 text-sm gap-1.5',
  md: 'h-11 px-6 text-sm gap-2',
  lg: 'h-12 px-8 text-base gap-2',
};

/** Bazaviy tugma — barcha yo'nalishlar (ommaviy va admin) shu komponentdan foydalanadi. */
export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  { className, variant = 'primary', size = 'md', isLoading = false, disabled, children, ...props },
  ref,
) {
  return (
    <button
      ref={ref}
      type={props.type ?? 'button'}
      className={cn(
        'inline-flex min-w-11 items-center justify-center rounded-full font-semibold',
        'transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-firuza-600',
        'disabled:cursor-not-allowed disabled:opacity-50',
        VARIANT_CLASSES[variant],
        SIZE_CLASSES[size],
        className,
      )}
      // `??` EMAS, `||`: chaqiruvchi `disabled={isLocked}` kabi ANIQ `false` bersa,
      // `false ?? isLoading` → `false` bo'lib, yuklanayotgan tugma ochiq qolardi va
      // ikki marta bosish ikkita so'rov yuborardi (audit jurnalida 176 ms farq bilan
      // ikkita `Auth.LoginSucceeded` aynan shundan). Yuklanish har doim bloklaydi.
      disabled={disabled || isLoading}
      aria-busy={isLoading || undefined}
      {...props}
    >
      {isLoading && <Loader2 className="size-4 animate-spin" aria-hidden="true" />}
      {children}
    </button>
  );
});
