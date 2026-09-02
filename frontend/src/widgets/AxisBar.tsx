import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';

/** 16 tipli model o'qlari — docs/03-psixologik-metodikalar.md, 2.2-bo'lim. */
export type AxisCode = 'EI' | 'SN' | 'TF' | 'JP';

export interface AxisBarProps {
  axisCode: AxisCode;
  /** Birinchi qutb foydasiga foiz — 0 = butunlay birinchi qutb, 100 = butunlay ikkinchi qutb. */
  pct: number;
  /** Tanlangan harf (backend tie-break qoidasi bilan hisoblangan — docs/03 2.2). */
  letter: string;
  /** `45 ≤ pct ≤ 55` — docs/03 2.2 "Borderline zona". */
  borderline: boolean;
  className?: string;
}

/** docs/03, 2.2-bo'lim jadvali: "0 tomon" / "100 tomon" harf va o'zbekcha nomi. */
const AXIS_POLES: Record<AxisCode, { zeroLetter: string; hundredLetter: string }> = {
  EI: { zeroLetter: 'I', hundredLetter: 'E' },
  SN: { zeroLetter: 'S', hundredLetter: 'N' },
  TF: { zeroLetter: 'T', hundredLetter: 'F' },
  JP: { zeroLetter: 'P', hundredLetter: 'J' },
};

function clampPct(pct: number): number {
  if (Number.isNaN(pct)) return 0;
  return Math.min(100, Math.max(0, pct));
}

/**
 * Bitta 16-tip o'qining gorizontal bari — markazda 50 chizig'i (docs/11 A-5 maket, P26 1-band).
 * Rang ballni baholamaydi: to'ldirilgan qism har doim bitta neytral/asosiy tusda, farq faqat
 * kenglikda va raqamda ko'rinadi (docs/11, 1-bo'lim).
 */
export function AxisBar({ axisCode, pct, letter, borderline, className }: AxisBarProps) {
  const { t } = useTranslation();
  const clamped = clampPct(pct);
  const pole = AXIS_POLES[axisCode];
  const zeroLabel = t(`widgets.axisBar.pole.${axisCode}.zero`);
  const hundredLabel = t(`widgets.axisBar.pole.${axisCode}.hundred`);
  const ariaLabel = t('widgets.axisBar.ariaLabel', {
    zeroLabel,
    hundredLabel,
    pct: clamped.toFixed(1),
    hundredLetter: pole.hundredLetter,
  });

  return (
    <div className={cn('flex flex-col gap-1.5', className)}>
      <div className="flex items-center justify-between gap-2 text-sm font-medium text-neutral-700">
        <span>
          {pole.zeroLetter} · {zeroLabel}
        </span>
        <span>
          {hundredLabel} · {pole.hundredLetter}
        </span>
      </div>

      <div
        role="img"
        aria-label={ariaLabel}
        className="relative h-3 w-full overflow-hidden rounded-full bg-neutral-200"
      >
        <div
          className="absolute inset-y-0 left-0 rounded-full bg-primary-500"
          style={{ width: `${String(clamped)}%` }}
        />
        <div
          aria-hidden="true"
          className="absolute inset-y-0 left-1/2 w-px -translate-x-1/2 bg-neutral-400"
        />
      </div>

      <div className="flex items-center justify-between gap-2">
        <span className="text-lg font-semibold text-neutral-900">
          {letter} — {clamped.toFixed(1)}%
        </span>
        {borderline && (
          <span className="rounded-full bg-neutral-100 px-2 py-0.5 text-xs font-medium text-neutral-600">
            {t('widgets.axisBar.borderline')}
          </span>
        )}
      </div>

      {/* Ekran o'quvchisi uchun jadval alternativi — docs/11, 4-bo'lim. */}
      <table className="sr-only">
        <caption>{ariaLabel}</caption>
        <thead>
          <tr>
            <th scope="col">{zeroLabel}</th>
            <th scope="col">{hundredLabel}</th>
            <th scope="col">{t('widgets.axisBar.resultColumn')}</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td>{(100 - clamped).toFixed(1)}%</td>
            <td>{clamped.toFixed(1)}%</td>
            <td>{letter}</td>
          </tr>
        </tbody>
      </table>
    </div>
  );
}
