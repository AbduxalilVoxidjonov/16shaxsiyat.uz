import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';
import { IndexGauge } from './IndexGauge';

/** Aktivlik shkalalari — docs/03, 5.1-bo'lim. */
export type ActivityScaleCode = 'MOT' | 'SELF' | 'SOCA' | 'ENG';

export interface ActivityBarsProps {
  scales: Record<ActivityScaleCode, number>;
  activityIndex: number;
  /** `ActivityIndex` uchun daraja matni — docs/03, 5.2-bo'lim jadvali ("Passiv" … "Juda faol"). */
  activityLevelText: string;
  className?: string;
}

const SCALE_ORDER: ActivityScaleCode[] = ['MOT', 'SELF', 'SOCA', 'ENG'];

function clampPct(pct: number): number {
  if (Number.isNaN(pct)) return 0;
  return Math.min(100, Math.max(0, pct));
}

/**
 * Aktivlik va motivatsiya — 4 bitta-tomonlama shkala bar + umumiy `ActivityIndex` gauge —
 * docs/11 A-5, P26 4-band. Rang bitta neytral/asosiy tusda (docs/11, 1-bo'lim).
 */
export function ActivityBars({
  scales,
  activityIndex,
  activityLevelText,
  className,
}: ActivityBarsProps) {
  const { t } = useTranslation();

  return (
    <div className={cn('flex flex-col gap-4 sm:flex-row sm:items-start sm:gap-6', className)}>
      <div className="flex flex-1 flex-col gap-3">
        {SCALE_ORDER.map((code) => {
          const value = clampPct(scales[code]);
          const label = t(`widgets.activityBars.scale.${code}`);
          const ariaLabel = t('widgets.activityBars.barAriaLabel', {
            label,
            value: value.toFixed(1),
          });
          return (
            <div key={code} data-testid={`activity-bar-${code}`} className="flex flex-col gap-1">
              <div className="flex items-center justify-between text-sm font-medium text-neutral-700">
                <span>{label}</span>
                <span className="font-semibold text-neutral-900">{value.toFixed(1)}%</span>
              </div>
              <div
                role="img"
                aria-label={ariaLabel}
                className="h-3 w-full overflow-hidden rounded-full bg-neutral-200"
              >
                <div
                  className="h-full rounded-full bg-primary-500"
                  style={{ width: `${String(value)}%` }}
                />
              </div>
            </div>
          );
        })}

        <table className="sr-only" data-testid="activity-bars-table">
          <caption>{t('widgets.activityBars.tableCaption')}</caption>
          <thead>
            <tr>
              <th scope="col">{t('widgets.activityBars.tableScaleColumn')}</th>
              <th scope="col">{t('widgets.activityBars.tableValueColumn')}</th>
            </tr>
          </thead>
          <tbody>
            {SCALE_ORDER.map((code) => (
              <tr key={code}>
                <td>{t(`widgets.activityBars.scale.${code}`)}</td>
                <td>{clampPct(scales[code]).toFixed(1)}%</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="sm:w-48">
        <IndexGauge
          value={activityIndex}
          label={t('widgets.activityBars.indexLabel')}
          levelText={activityLevelText}
        />
      </div>
    </div>
  );
}
