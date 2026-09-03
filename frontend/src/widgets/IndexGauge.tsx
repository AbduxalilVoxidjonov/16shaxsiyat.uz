import { useId } from 'react';
import { useTranslation } from 'react-i18next';
import { PolarAngleAxis, RadialBar, RadialBarChart, ResponsiveContainer } from 'recharts';
import { usePrefersReducedMotion } from '@/shared/hooks/usePrefersReducedMotion';
import { cn } from '@/shared/lib/cn';
import { VisuallyHidden } from '@/shared/ui/VisuallyHidden';

export interface IndexGaugeProps {
  /** 0–100 — masalan `MaturityIndex` yoki `ActivityIndex` (docs/03, 3.3/5.2-bo'lim). */
  value: number;
  /** Gauge nomi, masalan "Yetuklik indeksi". */
  label: string;
  /** Daraja matni, masalan "Yaxshi" (docs/03 jadvallaridan) — raqam bilan birga ko'rsatiladi. */
  levelText: string;
  className?: string;
}

/**
 * Yarim doira gauge (0–100) — docs/11 A-5, P26 5-band. Bitta neytral/asosiy rang har doim
 * bir xil — gauge rangi ballga qarab o'zgarmaydi (docs/11, 1-bo'lim: "past ball qizil bo'lmasin,
 * yuqori ball yashil bo'lmasin"); daraja faqat matn bilan ifodalanadi.
 */
export function IndexGauge({ value, label, levelText, className }: IndexGaugeProps) {
  const { t } = useTranslation();
  const titleId = useId();
  const reducedMotion = usePrefersReducedMotion();
  const clamped = Math.min(100, Math.max(0, value));
  const chartData = [{ value: clamped, fill: 'var(--color-primary-500)' }];
  const ariaLabel = t('widgets.indexGauge.ariaLabel', {
    label,
    value: clamped.toFixed(1),
    level: levelText,
  });

  return (
    <div className={cn('flex flex-col items-center', className)}>
      <h4 id={titleId} className="sr-only">
        {label}
      </h4>
      <div role="img" aria-label={ariaLabel} className="relative h-28 w-full max-w-48">
        <ResponsiveContainer width="100%" height="100%">
          <RadialBarChart
            cx="50%"
            cy="100%"
            innerRadius="70%"
            outerRadius="100%"
            barSize={14}
            startAngle={180}
            endAngle={0}
            data={chartData}
          >
            <PolarAngleAxis type="number" domain={[0, 100]} tick={false} axisLine={false} />
            <RadialBar
              background={{ fill: 'var(--color-neutral-200)' }}
              dataKey="value"
              cornerRadius={7}
              isAnimationActive={!reducedMotion}
            />
          </RadialBarChart>
        </ResponsiveContainer>
        <div
          data-testid="index-gauge-value"
          className="pointer-events-none absolute inset-x-0 bottom-0 flex flex-col items-center"
        >
          <span className="text-2xl font-bold text-neutral-900">{clamped.toFixed(1)}</span>
          <span className="text-xs text-neutral-500">{levelText}</span>
        </div>
      </div>

      {/* Ekran o'quvchisi uchun yashirin jadval alternativi — docs/11, 4-bo'lim. */}
      <VisuallyHidden>
        <table data-testid="index-gauge-table">
          <caption>{ariaLabel}</caption>
          <thead>
            <tr>
              <th scope="col">{t('widgets.indexGauge.tableLabelColumn')}</th>
              <th scope="col">{t('widgets.indexGauge.tableValueColumn')}</th>
              <th scope="col">{t('widgets.indexGauge.tableLevelColumn')}</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>{label}</td>
              <td>{clamped.toFixed(1)}</td>
              <td>{levelText}</td>
            </tr>
          </tbody>
        </table>
      </VisuallyHidden>
    </div>
  );
}
