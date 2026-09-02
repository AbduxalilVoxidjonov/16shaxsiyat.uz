import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import { Input } from '@/shared/ui/Input';
import { Button } from '@/shared/ui/Button';
import { hasActiveDashboardDateRange, readDashboardDateRange } from '../model/dateRangeFilters';

/**
 * Sana oralig'i filtri — docs/11 A-2: "Yuqorida sana oralig'i filtri (7 kun / 30 kun /
 * o'quv yili / ixtiyoriy)". Holat URL query'da (`from`/`to`) — vazifa ko'rsatmasi 6-band,
 * `StudentFiltersBar.tsx` bilan bir xil naqsh (`updateFilter`, `page` maydoni bu sahifada
 * yo'q shu sabab qayta o'rnatilmaydi).
 */
export function DateRangeFilter() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();
  const filters = readDashboardDateRange(searchParams);

  function updateRange(key: 'from' | 'to', value: string) {
    setSearchParams((prev) => {
      const params = new URLSearchParams(prev);
      if (value) {
        params.set(key, value);
      } else {
        params.delete(key);
      }
      return params;
    });
  }

  function applyPreset(days: number) {
    const to = new Date();
    const from = new Date();
    from.setUTCDate(from.getUTCDate() - days);
    setSearchParams((prev) => {
      const params = new URLSearchParams(prev);
      params.set('from', from.toISOString().slice(0, 10));
      params.set('to', to.toISOString().slice(0, 10));
      return params;
    });
  }

  function clearRange() {
    setSearchParams((prev) => {
      const params = new URLSearchParams(prev);
      params.delete('from');
      params.delete('to');
      return params;
    });
  }

  return (
    <div
      data-testid="dashboard-date-range"
      className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-end"
    >
      <div className="flex flex-wrap gap-2">
        <Button variant="outline" size="sm" onClick={() => applyPreset(7)}>
          {t('dashboard.dateRange.presets.last7')}
        </Button>
        <Button variant="outline" size="sm" onClick={() => applyPreset(30)}>
          {t('dashboard.dateRange.presets.last30')}
        </Button>
      </div>

      <div className="w-full sm:w-40">
        <Input
          type="date"
          label={t('dashboard.dateRange.fromLabel')}
          value={filters.from}
          onChange={(event) => updateRange('from', event.target.value)}
        />
      </div>

      <div className="w-full sm:w-40">
        <Input
          type="date"
          label={t('dashboard.dateRange.toLabel')}
          value={filters.to}
          onChange={(event) => updateRange('to', event.target.value)}
        />
      </div>

      {hasActiveDashboardDateRange(filters) && (
        <Button variant="ghost" size="sm" onClick={clearRange}>
          {t('dashboard.dateRange.clear')}
        </Button>
      )}
    </div>
  );
}
