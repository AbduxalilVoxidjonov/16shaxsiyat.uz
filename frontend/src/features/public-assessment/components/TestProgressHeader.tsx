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
    <header className="sticky top-0 z-10 -mx-4 border-b border-neutral-200 bg-white/95 px-4 py-3 backdrop-blur">
      <div className="mb-1.5 flex items-center justify-between gap-2">
        <h1 className="truncate text-base font-semibold text-neutral-900">{testName}</h1>
        <div
          className="flex shrink-0 items-center gap-2 text-sm text-neutral-500"
          aria-label={t('test.blockIndicatorLabel', { current: blockIndex, total: totalBlocks })}
        >
          <span className="flex gap-1" aria-hidden="true">
            {Array.from({ length: totalBlocks }, (_, index) => (
              <span
                key={index}
                className={cn(
                  'size-2 rounded-full',
                  index < blockIndex ? 'bg-primary-600' : 'bg-neutral-200',
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
        className="h-2 w-full overflow-hidden rounded-full bg-neutral-100"
      >
        <div
          className="h-full rounded-full bg-primary-600 transition-[width] motion-reduce:transition-none"
          style={{ width: `${String(percent)}%` }}
        />
      </div>
      <p className="mt-1 text-right text-xs text-neutral-500">
        {t('test.progressText', { answered, total })}
      </p>
    </header>
  );
}
