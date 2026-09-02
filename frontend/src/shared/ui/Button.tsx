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

const VARIANT_CLASSES: Record<ButtonVariant, string> = {
  primary: 'bg-primary-600 text-white hover:bg-primary-700 focus-visible:outline-primary-600',
  secondary: 'bg-neutral-100 text-neutral-900 hover:bg-neutral-200',
  outline: 'border border-neutral-300 text-neutral-900 hover:bg-neutral-50',
  ghost: 'text-neutral-700 hover:bg-neutral-100',
  danger: 'bg-danger-600 text-white hover:bg-danger-700 focus-visible:outline-danger-600',
};

const SIZE_CLASSES: Record<ButtonSize, string> = {
  sm: 'h-9 px-3 text-sm gap-1.5',
  md: 'h-11 px-4 text-sm gap-2',
  lg: 'h-12 px-6 text-base gap-2',
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
        'inline-flex min-w-11 items-center justify-center rounded-lg font-medium',
        'transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2',
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
