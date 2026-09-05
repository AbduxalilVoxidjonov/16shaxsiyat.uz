import type { HTMLAttributes } from 'react';
import { cn } from '@/shared/lib/cn';

export type SkeletonProps = HTMLAttributes<HTMLDivElement>;

/** Yuklanish holati uchun soyabon (pulsing) joy egallovchi. Ekran o'quvchisidan yashiriladi. */
export function Skeleton({ className, ...props }: SkeletonProps) {
  return (
    <div
      className={cn('animate-pulse rounded-lg bg-line', className)}
      aria-hidden="true"
      {...props}
    />
  );
}
