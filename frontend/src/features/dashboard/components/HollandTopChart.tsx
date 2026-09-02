import { useTranslation } from 'react-i18next';
import { Card } from '@/shared/ui/Card';
import { DistributionBarList } from './DistributionBarList';
import type { HollandTopItem } from '../model/types';

export interface HollandTopChartProps {
  items: HollandTopItem[];
}

/** Kasb qiziqishlari (Holland) top kodlari — docs/11 A-2 (`hollandTop[]`, docs/07 3.6). */
export function HollandTopChart({ items }: HollandTopChartProps) {
  const { t } = useTranslation();
  const rows = items.map((item) => ({ key: item.code, label: item.code, count: item.count }));
  const ariaLabel = t('dashboard.charts.hollandAriaLabel', {
    values: rows.map((row) => `${row.label} ${String(row.count)}`).join(', '),
  });

  return (
    <Card title={t('dashboard.charts.hollandHeading')}>
      <DistributionBarList
        rows={rows}
        ariaLabel={ariaLabel}
        tableCaption={t('dashboard.charts.hollandHeading')}
        labelColumnHeader={t('dashboard.charts.tableCodeColumn')}
        countColumnHeader={t('dashboard.charts.tableCountColumn')}
        emptyTitle={t('dashboard.charts.hollandEmptyTitle')}
        emptyDescription={t('dashboard.charts.hollandEmptyDescription')}
        testId="holland-top"
      />
    </Card>
  );
}
