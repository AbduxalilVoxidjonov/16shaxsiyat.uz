import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';

export interface TestProgressHeaderProps {
  testName: string;
  /** Joriy blok tartib raqami (1 dan boshlab). */
  blockIndex: number;
  totalBlocks: number;
  answered: number;
  total: number;
}

/**
 * E-3 sticky header — test nomi, blok indikatori (●●○○), umumiy progress bar
 * (docs/11 E-3 wireframe). Rangga qo'shimcha raqamli qiymat ham ko'rsatiladi (a11y, docs/11 4-bo'lim).
 *
 * Manfiy gorizontal margin (`-mx-5 sm:-mx-8`) `PublicLayout` ning `main` paddingini qoplaydi —
 * panel kontent kengligidan chetga chiqib, ekran bo'ylab yaxlit chiziq hosil qiladi.
 */
export function TestProgressHeader({
  testName,
  blockIndex,
  totalBlocks,
  answered,
  total,
}: TestProgressHeaderProps) {
  const { t } = useTranslation();
  const percent = total > 0 ? Math.round((answered / total) * 100) : 0;

  return (
    <header className="sticky top-0 z-10 -mx-5 border-b border-line bg-paper/90 px-5 py-3.5 backdrop-blur-xl sm:-mx-8 sm:px-8">
      <div className="mb-2 flex items-center justify-between gap-3">
        <h1 className="truncate font-display text-base font-extrabold tracking-tight text-ink">
          {testName}
        </h1>
        <div
          className="flex shrink-0 items-center gap-2 text-[13px] font-semibold text-ink-soft"
          aria-label={t('test.blockIndicatorLabel', { current: blockIndex, total: totalBlocks })}
        >
          <span className="flex gap-1" aria-hidden="true">
            {Array.from({ length: totalBlocks }, (_, index) => (
              <span
                key={index}
                className={cn(
                  'size-2 rounded-full transition-colors',
                  index < blockIndex ? 'bg-firuza-500' : 'bg-line-strong',
                )}
              />
            ))}
          </span>
          <span>{t('test.blockIndicator', { current: blockIndex, total: totalBlocks })}</span>
        </div>
      </div>
      <div
        role="progressbar"
        aria-valuenow={percent}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={t('test.progressLabel', { answered, total })}
        className="h-2 w-full overflow-hidden rounded-full bg-paper-deep"
      >
        <div
          className="h-full rounded-full bg-linear-to-r from-firuza-400 to-firuza-600 transition-[width] duration-500 ease-out motion-reduce:transition-none"
          style={{ width: `${String(percent)}%` }}
        />
      </div>
      <p className="mt-1.5 text-right text-xs font-semibold text-ink-soft">
        {t('test.progressText', { answered, total })}
      </p>
    </header>
  );
}
