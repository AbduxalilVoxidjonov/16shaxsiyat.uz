import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { Input } from '@/shared/ui/Input';
import { Select } from '@/shared/ui/Select';
import { useDebounce } from '@/shared/hooks/useDebounce';
import { UZBEKISTAN_REGIONS } from '../model/regions';
import { readSchoolsFilters } from '../model/schoolsFilters';

const SEARCH_DEBOUNCE_MS = 400;

/**
 * Qidiruv (400 ms debounce) + viloyat + faollik filtrlari — barchasi URL query'da
 * (`docs/10`, 5.3-bo'lim; CLAUDE.md "MAXSUS DIQQAT" 4). Sahifa yangilanganda holat qoladi,
 * brauzer orqaga tugmasi ishlaydi (`useSearchParams` — react-router history bilan sinxron).
 */
export function SchoolFiltersBar() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();
  const filters = readSchoolsFilters(searchParams);

  const [searchInput, setSearchInput] = useState(filters.search);
  // Boshqa joydan (masalan orqaga tugmasi) URL o'zgarsa, matn maydoni ham sinxronlansin —
  // effekt EMAS, render vaqtida moslashtirish (React qo'llanmasi: "Adjusting state when a
  // prop changes"), `react-hooks/set-state-in-effect` qoidasi effekt ichida sinxron
  // `setState`ni taqiqlaydi.
  const [syncedUrlSearch, setSyncedUrlSearch] = useState(filters.search);
  if (filters.search !== syncedUrlSearch) {
    setSyncedUrlSearch(filters.search);
    setSearchInput(filters.search);
  }

  const debouncedSearch = useDebounce(searchInput, SEARCH_DEBOUNCE_MS);

  useEffect(() => {
    const current = searchParams.get('search') ?? '';
    if (current === debouncedSearch) return;
    setSearchParams(
      (prev) => {
        const params = new URLSearchParams(prev);
        if (debouncedSearch) {
          params.set('search', debouncedSearch);
        } else {
          params.delete('search');
        }
        params.set('page', '1');
        return params;
      },
      { replace: true },
    );
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch]);

  function updateFilter(key: 'region' | 'active', value: string) {
    setSearchParams((prev) => {
      const params = new URLSearchParams(prev);
      if (value) {
        params.set(key, value);
      } else {
        params.delete(key);
      }
      params.set('page', '1');
      return params;
    });
  }

  return (
    <div
      data-testid="schools-filters"
      className="flex flex-col gap-3 sm:flex-row sm:items-end sm:flex-wrap"
    >
      <div className="w-full sm:max-w-xs">
        <Input
          label={t('schools.filters.searchLabel')}
          placeholder={t('schools.filters.searchPlaceholder')}
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
      </div>

      <div className="w-full sm:w-56">
        <Select
          label={t('schools.filters.regionLabel')}
          placeholder={t('schools.filters.regionPlaceholder')}
          value={filters.region}
          onChange={(event) => updateFilter('region', event.target.value)}
          options={UZBEKISTAN_REGIONS.map((region) => ({ value: region, label: region }))}
        />
      </div>

      <div className="w-full sm:w-44">
        <Select
          label={t('schools.filters.statusLabel')}
          value={filters.active}
          onChange={(event) => updateFilter('active', event.target.value)}
          options={[
            { value: '', label: t('schools.filters.statusAll') },
            { value: 'true', label: t('schools.filters.statusActive') },
            { value: 'false', label: t('schools.filters.statusInactive') },
          ]}
        />
      </div>
    </div>
  );
}
