import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import { Input } from '@/shared/ui/Input';
import { Select } from '@/shared/ui/Select';
import { Button } from '@/shared/ui/Button';
import { AUDIT_ACTION_OPTIONS, AUDIT_ENTITY_TYPE_OPTIONS } from '../model/auditActions';
import { hasActiveAuditFilters, readAuditFilters } from '../model/auditFilters';

/**
 * Audit filtrlari — `docs/11` A-9: "filtr: harakat turi, obyekt, sana". Barcha holat URL
 * query'da (`SchoolFiltersBar.tsx`/`DateRangeFilter.tsx` bilan bir xil naqsh) — sahifa
 * ulashiladigan bo'lsin, deep-link ishlasin (`prompts/28` MAXSUS DIQQAT #6).
 */
export function AuditFiltersBar() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();
  const filters = readAuditFilters(searchParams);

  function updateFilter(key: 'action' | 'entityType' | 'from' | 'to', value: string) {
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

  function clearFilters() {
    setSearchParams((prev) => {
      const params = new URLSearchParams(prev);
      params.delete('action');
      params.delete('entityType');
      params.delete('from');
      params.delete('to');
      params.set('page', '1');
      return params;
    });
  }

  return (
    <div
      data-testid="audit-filters"
      className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-end"
    >
      <div className="w-full sm:w-64">
        <Select
          label={t('audit.filters.actionLabel')}
          placeholder={t('audit.filters.actionAll')}
          value={filters.action}
          onChange={(event) => updateFilter('action', event.target.value)}
          options={[
            { value: '', label: t('audit.filters.actionAll') },
            ...AUDIT_ACTION_OPTIONS.map((option) => ({ value: option.value, label: t(option.labelKey) })),
          ]}
        />
      </div>

      <div className="w-full sm:w-56">
        <Select
          label={t('audit.filters.entityTypeLabel')}
          placeholder={t('audit.filters.entityTypeAll')}
          value={filters.entityType}
          onChange={(event) => updateFilter('entityType', event.target.value)}
          options={[
            { value: '', label: t('audit.filters.entityTypeAll') },
            ...AUDIT_ENTITY_TYPE_OPTIONS.map((option) => ({
              value: option.value,
              label: t(option.labelKey),
            })),
          ]}
        />
      </div>

      <div className="w-full sm:w-40">
        <Input
          type="date"
          label={t('audit.filters.fromLabel')}
          value={filters.from}
          onChange={(event) => updateFilter('from', event.target.value)}
        />
      </div>

      <div className="w-full sm:w-40">
        <Input
          type="date"
          label={t('audit.filters.toLabel')}
          value={filters.to}
          onChange={(event) => updateFilter('to', event.target.value)}
        />
      </div>

      {hasActiveAuditFilters(filters) && (
        <Button variant="ghost" size="sm" onClick={clearFilters}>
          {t('audit.filters.clear')}
        </Button>
      )}
    </div>
  );
}
