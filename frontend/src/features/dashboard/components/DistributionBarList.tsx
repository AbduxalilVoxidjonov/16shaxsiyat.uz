import { useTranslation } from 'react-i18next';
import { EmptyState } from '@/shared/ui/EmptyState';

export interface DistributionBarRow {
  key: string;
  label: string;
  count: number;
}

export interface DistributionBarListProps {
  rows: DistributionBarRow[];
  ariaLabel: string;
  tableCaption: string;
  labelColumnHeader: string;
  countColumnHeader: string;
  emptyTitle: string;
  emptyDescription: string;
  testId: string;
}

/**
 * Taqsimot ro'yxati — vazifa ko'rsatmasi 4-band: "shaxsiyat tiplari, aktivlik darajalari,
 * Holland top". Bitta neytral/asosiy rangda gorizontal bar (docs/11, 1-bo'lim — rang
 * darajani baholamaydi), qiymat raqam bilan yoniga yoziladi, jami ichidagi ulushi
 * ikkinchi darajali matn bilan. `PersonalityDistributionChart`/`ActivityDistributionChart`/
 * `HollandTopChart` uchun umumiy taqdimot qatlami (Recharts o'rniga oddiy div-bar — 16
 * ustunli shaxsiyat taqsimotida X o'qiga sig'dirish uchun `RiasecChart`dagi kabi Recharts
 * `BarChart` noqulay bo'lardi, gorizontal ro'yxat mobil va ko'p elementli holatda
 * o'qilishi osonroq).
 */
export function DistributionBarList({
  rows,
  ariaLabel,
  tableCaption,
  labelColumnHeader,
  countColumnHeader,
  emptyTitle,
  emptyDescription,
  testId,
}: DistributionBarListProps) {
  const { t } = useTranslation();

  if (rows.length === 0) {
    return <EmptyState title={emptyTitle} description={emptyDescription} />;
  }

  const total = rows.reduce((sum, row) => sum + row.count, 0);
  const maxCount = Math.max(...rows.map((row) => row.count), 1);

  return (
    <div data-testid={testId}>
      <div
        role="img"
        aria-label={ariaLabel}
        data-testid={`${testId}-list`}
        className="flex flex-col gap-2.5"
      >
        {rows.map((row) => {
          const pct = Math.min(100, Math.max(0, (row.count / maxCount) * 100));
          const sharePct = total > 0 ? (row.count / total) * 100 : 0;
          return (
            <div key={row.key} className="flex flex-col gap-1">
              <div className="flex items-baseline justify-between gap-2 text-sm">
                <span className="font-medium text-neutral-700">{row.label}</span>
                <span className="flex items-baseline gap-1.5">
                  <span className="font-semibold text-neutral-900">{row.count}</span>
                  <span className="text-xs text-neutral-500">
                    {t('dashboard.charts.shareOfTotal', { value: sharePct.toFixed(0) })}
                  </span>
                </span>
              </div>
              <div className="h-2.5 w-full overflow-hidden rounded-full bg-neutral-200">
                <div
                  className="h-full rounded-full bg-primary-500"
                  style={{ width: `${String(pct)}%` }}
                />
              </div>
            </div>
          );
        })}
      </div>

      <table className="sr-only" data-testid={`${testId}-table`}>
        <caption>{tableCaption}</caption>
        <thead>
          <tr>
            <th scope="col">{labelColumnHeader}</th>
            <th scope="col">{countColumnHeader}</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.key}>
              <td>{row.label}</td>
              <td>{row.count}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
