import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { Input } from '@/shared/ui/Input';
import { Select } from '@/shared/ui/Select';
import { useDebounce } from '@/shared/hooks/useDebounce';
import { readProgramsFilters } from '../model/programsFilters';

const SEARCH_DEBOUNCE_MS = 400;

/** Qidiruv + holat + faollik filtrlari — barchasi URL query'da (`docs/10`, 5.3-bo'lim). */
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

  function updateFilter(key: 'status' | 'active', value: string) {
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

      <div className="w-full sm:w-48">
        <Select
          label={t('programs.filters.statusLabel')}
          value={filters.status}
          onChange={(event) => updateFilter('status', event.target.value)}
          options={[
            { value: '', label: t('programs.filters.statusAll') },
            { value: 'Draft', label: t('programs.status.draft') },
            { value: 'Published', label: t('programs.status.published') },
            { value: 'Archived', label: t('programs.status.archived') },
          ]}
        />
      </div>

      <div className="w-full sm:w-44">
        <Select
          label={t('programs.filters.activeLabel')}
          value={filters.active}
          onChange={(event) => updateFilter('active', event.target.value)}
          options={[
            { value: '', label: t('programs.filters.activeAll') },
            { value: 'true', label: t('programs.filters.activeOnly') },
            { value: 'false', label: t('programs.filters.inactiveOnly') },
          ]}
        />
      </div>
    </div>
  );
}
