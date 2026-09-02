import { useTranslation } from 'react-i18next';
import { Card } from '@/shared/ui/Card';
import { DistributionBarList } from './DistributionBarList';
import type { ActivityDistributionItem } from '../model/types';

export interface ActivityDistributionChartProps {
  items: ActivityDistributionItem[];
}

/**
 * Aktivlik darajalari taqsimoti — docs/11 A-2 wireframe ("donut, 5 segment"). Doira
 * diagramma o'rniga bar-ro'yxat ishlatiladi (`DistributionBarList` izohiga qarang) —
 * bir xil ma'lumotni ko'rsatadi, lekin son har doim ko'rinadi va rang darajani
 * bildirmaydi (docs/11, 1-bo'lim — donutda darajalarni rang gradienti bilan chizish
 * "Passiv qizil, Juda faol yashil" degan hukmga o'xshab qolish xavfi bor edi).
 * `level` nomi noma'lum bo'lsa ham (backend yangi qiymat qo'shsa) xom kod ko'rsatiladi
 * — sahifa yiqilmaydi.
 */
export function ActivityDistributionChart({ items }: ActivityDistributionChartProps) {
  const { t } = useTranslation();
  const rows = items.map((item) => {
    const key = `dashboard.charts.activityLevel.${item.level}`;
    const label = t(key, { defaultValue: item.level });
    return { key: item.level, label, count: item.count };
  });
  const ariaLabel = t('dashboard.charts.activityAriaLabel', {
    values: rows.map((row) => `${row.label} ${String(row.count)}`).join(', '),
  });

  return (
    <Card title={t('dashboard.charts.activityHeading')}>
      <DistributionBarList
        rows={rows}
        ariaLabel={ariaLabel}
        tableCaption={t('dashboard.charts.activityHeading')}
        labelColumnHeader={t('dashboard.charts.tableLevelColumn')}
        countColumnHeader={t('dashboard.charts.tableCountColumn')}
        emptyTitle={t('dashboard.charts.activityEmptyTitle')}
        emptyDescription={t('dashboard.charts.activityEmptyDescription')}
        testId="activity-distribution"
      />
    </Card>
  );
}
