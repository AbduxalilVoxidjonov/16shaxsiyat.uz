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
      className={cn('rounded-xl border border-neutral-200 bg-white p-4 shadow-sm', className)}
      {...props}
    >
      {(title ?? actions) && (
        <div className="mb-3 flex items-center justify-between gap-2">
          {title && <h3 className="text-base font-semibold text-neutral-900">{title}</h3>}
          {actions}
        </div>
      )}
      {children}
    </div>
  );
}
