import { cn } from '@/shared/lib/cn';

export interface GirihStarProps {
  className?: string;
  /** SVG chiziqlarining qalinligi. */
  strokeWidth?: number;
  /** `false` bo'lsa markazdagi doira chizilmaydi (ichma-ich joylashgan naqsh qatlamlari uchun). */
  withCircle?: boolean;
}

/**
 * Girih naqshi — bir-birining ustiga 45° burilgan ikki kvadrat va ixtiyoriy markaziy doira,
 * birgalikda sakkiz qirrali yulduzcha hosil qiladi.
 *
 * Rang `currentColor` orqali olinadi: ota elementga `text-firuza-500` kabi klass bering.
 * Sof dekorativ element — shu sabab `aria-hidden`.
 */
export function GirihStar({ className, strokeWidth = 2, withCircle = true }: GirihStarProps) {
  return (
    <svg
      viewBox="0 0 100 100"
      fill="none"
      className={cn(className)}
      aria-hidden="true"
      focusable="false"
    >
      <rect x="20" y="20" width="60" height="60" stroke="currentColor" strokeWidth={strokeWidth} />
      <rect
        x="20"
        y="20"
        width="60"
        height="60"
        stroke="currentColor"
        strokeWidth={strokeWidth}
        transform="rotate(45 50 50)"
      />
      {withCircle && (
        <circle cx="50" cy="50" r="21" stroke="currentColor" strokeWidth={strokeWidth} />
      )}
    </svg>
  );
}
