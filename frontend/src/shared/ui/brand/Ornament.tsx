import { cn } from '@/shared/lib/cn';

export interface OrnamentProps {
  className?: string;
}

/**
 * Bo'lim ajratkichi — ikki tomonga so'nuvchi chiziq va o'rtasida kichik girih belgisi.
 * Sof dekorativ, semantik ma'no tashimaydi.
 */
export function Divider({ className }: OrnamentProps) {
  return (
    <div className={cn('flex items-center gap-4', className)} aria-hidden="true">
      <span className="h-px flex-1 bg-linear-to-r from-transparent to-line-strong" />
      <svg viewBox="0 0 40 12" className="h-3 w-10 text-line-strong" fill="none" focusable="false">
        <path d="M2 6h9M29 6h9" stroke="currentColor" strokeWidth="1.2" />
        <rect x="16" y="2" width="8" height="8" stroke="currentColor" strokeWidth="1.2" />
        <rect
          x="16"
          y="2"
          width="8"
          height="8"
          stroke="currentColor"
          strokeWidth="1.2"
          transform="rotate(45 20 6)"
        />
      </svg>
      <span className="h-px flex-1 bg-linear-to-l from-transparent to-line-strong" />
    </div>
  );
}

/**
 * Orqa fondagi xiralashtirilgan rangli dog'. Rang, o'lcham va joylashuv `className` orqali
 * beriladi, masalan: `<Blob className="-top-24 left-10 size-72 bg-firuza-300/30" />`.
 * Ota element `relative` bo'lishi kerak.
 */
export function Blob({ className }: OrnamentProps) {
  return (
    <div
      aria-hidden="true"
      className={cn('pointer-events-none absolute rounded-full blur-3xl', className)}
    />
  );
}

/** Gumbaz shaklidagi yoy chizig'i — bo'lim tepasidagi me'moriy urg'u uchun. */
export function ArchTop({ className }: OrnamentProps) {
  return (
    <svg
      viewBox="0 0 120 60"
      fill="none"
      className={cn(className)}
      aria-hidden="true"
      focusable="false"
    >
      <path
        d="M4 60V34C4 16.3 29.1 2 60 2s56 14.3 56 32v26"
        stroke="currentColor"
        strokeWidth="1.5"
      />
    </svg>
  );
}
