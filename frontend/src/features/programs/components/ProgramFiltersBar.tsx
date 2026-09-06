import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { Input } from '@/shared/ui/Input';
import { Select } from '@/shared/ui/Select';
import { useDebounce } from '@/shared/hooks/useDebounce';
import { readProgramsFilters } from '../model/programsFilters';
import { PROGRAM_STATE_VALUES } from '../model/types';

const SEARCH_DEBOUNCE_MS = 400;

/**
 * Qidiruv + YAGONA holat filtri — ikkalasi ham URL query'da (`docs/10`, 5.3-bo'lim).
 *
 * **2026-09-06:** ilgari ikkita tanlagich bor edi — "Holat" (`Draft`/`Published`/`Archived`)
 * va "Faollik" (ha/yo'q). Ular bir-birini inkor qiladigan juftlikni tanlash imkonini berardi
 * (`Arxiv` + `Faqat faol` — doim bo'sh ro'yxat) va ro'yxatdagi ikkita belgi bilan birga
 * "bitta dastur ikkita holatda" degan taassurot tug'dirardi.
 */
export function ProgramFiltersBar() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();
  const filters = readProgramsFilters(searchParams);

  const [searchInput, setSearchInput] = useState(filters.search);
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

  function updateStateFilter(value: string) {
    setSearchParams((prev) => {
      const params = new URLSearchParams(prev);
      if (value) {
        params.set('state', value);
      } else {
        params.delete('state');
      }
      params.set('page', '1');
      return params;
    });
  }

  return (
    <div
      data-testid="programs-filters"
      className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-end"
    >
      <div className="w-full sm:max-w-xs">
        <Input
          label={t('programs.filters.searchLabel')}
          placeholder={t('programs.filters.searchPlaceholder')}
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
      </div>

      <div className="w-full sm:w-52">
        <Select
          label={t('programs.filters.stateLabel')}
          value={filters.state}
          onChange={(event) => updateStateFilter(event.target.value)}
          options={[
            { value: '', label: t('programs.filters.stateAll') },
            ...PROGRAM_STATE_VALUES.map((state) => ({
              value: state,
              label: t(`programs.state.${state.toLowerCase()}`),
            })),
          ]}
        />
      </div>
    </div>
  );
}
