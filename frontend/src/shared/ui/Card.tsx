import type { HTMLAttributes, ReactNode } from 'react';
import { cn } from '@/shared/lib/cn';

export interface CardProps extends Omit<HTMLAttributes<HTMLDivElement>, 'title'> {
  title?: ReactNode;
  actions?: ReactNode;
}

/** Yig'ma karta — statistika, bo'lim va widget konteynerlari uchun bazaviy element. */
export function Card({ className, title, actions, children, ...props }: CardProps) {
  return (
    <div
      // `rounded-2xl` (1rem) ATAYLAB, `.card` dagi `rounded-4xl` (2rem) EMAS: admin
      // panelda kartalar kichik va zich joylashadi (statistika kataklari, filtr paneli),
      // 2rem radius ularni "shishirib" yuboradi va jadval bilan yonma-yon g'alati ko'rinadi.
      className={cn('rounded-2xl border border-line bg-paper-card p-4 shadow-soft', className)}
      {...props}
    >
      {(title ?? actions) && (
        <div className="mb-3 flex items-center justify-between gap-2">
          {title && <h3 className="font-display text-base font-bold text-ink">{title}</h3>}
          {actions}
        </div>
      )}
      {children}
    </div>
  );
}
