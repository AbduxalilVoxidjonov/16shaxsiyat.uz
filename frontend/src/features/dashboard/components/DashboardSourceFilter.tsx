import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import { cn } from '@/shared/lib/cn';
import {
  DASHBOARD_SOURCE_VALUES,
  readDashboardSource,
  type DashboardSource,
} from '../model/dashboardSource';

/**
 * Manba almashtirgichi (P48) — "Maktablar" / "Ommaviy makon".
 *
 * Radio guruh sifatida qurilgan (segment ko'rinishida): klaviatura bilan o'q tugmalari
 * ishlaydi va skrinrider tanlangan variantni o'zi e'lon qiladi — bir nechta `<button>`
 * bilan bu bepul kelmasdi.
 */
export function DashboardSourceFilter() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();
  const source = readDashboardSource(searchParams);

  function selectSource(next: DashboardSource) {
    setSearchParams((prev) => {
      const params = new URLSearchParams(prev);
      params.set('source', next);
      return params;
    });
  }

  return (
    <fieldset data-testid="dashboard-source-filter" className="flex flex-col gap-1.5">
      <legend className="text-sm font-medium text-ink-soft">{t('dashboard.source.legend')}</legend>
      <div className="inline-flex w-fit rounded-full border border-line bg-paper-deep p-1">
        {DASHBOARD_SOURCE_VALUES.map((value) => (
          <label
            key={value}
            className={cn(
              'cursor-pointer rounded-full px-4 py-1.5 text-sm font-medium text-ink-soft transition-colors',
              'has-[:focus-visible]:outline has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-firuza-600',
              source === value && 'bg-firuza-600 font-semibold text-white shadow-soft',
            )}
          >
            <input
              type="radio"
              name="dashboard-source"
              value={value}
              checked={source === value}
              onChange={() => selectSource(value)}
              className="sr-only"
            />
            {value === 'school' ? t('dashboard.source.school') : t('dashboard.source.public')}
          </label>
        ))}
      </div>
    </fieldset>
  );
}
