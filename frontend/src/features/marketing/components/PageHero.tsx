import { GirihStar, PatternBackdrop } from '@/shared/ui/brand';
import { cn } from '@/shared/lib/cn';

export interface PageHeroProps {
  /** Sarlavha ustidagi kichik bosh harfli yozuv. */
  eyebrow: string;
  /** Sahifaning yagona `<h1>` matni. */
  heading: string;
  /** Sarlavha ostidagi kirish matni. */
  lead: string;
  /** `true` bo'lsa matn markazga tekislanadi (standart: chapga). */
  centered?: boolean;
  className?: string;
}

/**
 * Ichki tanishtiruv sahifalarining bir xil boshlanish bloki — qumrang fon, girih naqsh va
 * aylanuvchi yulduz.
 *
 * Uchala sahifada (metodika, biz haqimizda, aloqa) bir xil takrorlanadigan JSX shu yerga
 * chiqarilgan: sarlavha o'lchami va bo'shliqlar bir joyda o'zgaradi.
 */
export function PageHero({ eyebrow, heading, lead, centered = false, className }: PageHeroProps) {
  return (
    <section
      className={cn('relative overflow-hidden border-b border-line bg-paper-deep', className)}
    >
      <PatternBackdrop className="opacity-50" />
      <GirihStar
        className="animate-spin-slow pointer-events-none absolute top-1/2 -right-24 size-80 -translate-y-1/2 text-line-strong opacity-40"
        strokeWidth={1}
      />
      <div className={cn('wrap relative py-16 sm:py-20', centered && 'text-center')}>
        <p className="eyebrow text-ink-soft">{eyebrow}</p>
        <h1
          className={cn(
            'balance font-display mt-3 text-4xl font-extrabold sm:text-6xl',
            centered ? 'mx-auto max-w-3xl' : 'max-w-3xl',
          )}
        >
          {heading}
        </h1>
        <p className={cn('lead mt-6 max-w-2xl', centered && 'mx-auto')}>{lead}</p>
      </div>
    </section>
  );
}
