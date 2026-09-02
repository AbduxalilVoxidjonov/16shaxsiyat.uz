import { useTranslation } from 'react-i18next';
import { Card } from '@/shared/ui/Card';
import { DistributionBarList } from './DistributionBarList';
import type { PersonalityDistributionItem } from '../model/types';

export interface PersonalityDistributionChartProps {
  items: PersonalityDistributionItem[];
}

/** Shaxsiyat tiplari taqsimoti — docs/11 A-2 wireframe ("bar chart, 16 ustun"). */
export function PersonalityDistributionChart({ items }: PersonalityDistributionChartProps) {
  const { t } = useTranslation();
  const rows = items.map((item) => ({ key: item.type, label: item.type, count: item.count }));
  const ariaLabel = t('dashboard.charts.personalityAriaLabel', {
    values: rows.map((row) => `${row.label} ${String(row.count)}`).join(', '),
  });

  return (
    <Card title={t('dashboard.charts.personalityHeading')}>
      <DistributionBarList
        rows={rows}
        ariaLabel={ariaLabel}
        tableCaption={t('dashboard.charts.personalityHeading')}
        labelColumnHeader={t('dashboard.charts.tableTypeColumn')}
        countColumnHeader={t('dashboard.charts.tableCountColumn')}
        emptyTitle={t('dashboard.charts.personalityEmptyTitle')}
        emptyDescription={t('dashboard.charts.personalityEmptyDescription')}
        testId="personality-distribution"
      />
    </Card>
  );
}
