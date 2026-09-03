import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { Select } from '@/shared/ui/Select';
import { useSchoolOptionsQuery } from '../api/useSchoolOptionsQuery';
import {
  ASSESSMENTS_FILTER_KEYS,
  hasActiveAssessmentsFilters,
  readAssessmentsFilters,
  type AssessmentsFilterKey,
} from '../model/assessmentsFilters';
import { ASSESSMENT_STATUS_VALUES } from '../model/types';

/**
 * Sessiyalar filtr paneli — `docs/11` A-6 ("Ro'yxat + holat filtri") va `prompts/15`
 * filtrlari: holat, maktab, sana oralig'i. Barcha holat URL query'da (`CLAUDE.md`
 * "MAXSUS DIQQAT" 2) — shu sabab boshqaruv panelidagi `?status=Analyzing` havolasi
 * darhol filtr sifatida qo'llanadi va orqaga tugmasi ishlaydi.
 */
export function AssessmentFiltersBar() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();
  const filters = readAssessmentsFilters(searchParams);
  const schoolOptionsQuery = useSchoolOptionsQuery();

  function updateFilter(key: AssessmentsFilterKey, value: string) {
    setSearchParams((prev) => {
      const params = new URLSearchParams(prev);
      if (value) {
        params.set(key, value);
      } else {
        params.delete(key);
      }
      // Filtr o'zgarganda birinchi sahifaga qaytiladi (`prompts/24` qoidasi).
      params.set('page', '1');
      return params;
    });
  }

  function clearAll() {
    setSearchParams((prev) => {
      const params = new URLSearchParams(prev);
      for (const key of ASSESSMENTS_FILTER_KEYS) {
        params.delete(key);
      }
      params.set('page', '1');
      return params;
    });
  }

  return (
    <div className="flex flex-col gap-3 rounded-xl border border-neutral-200 bg-white p-4">
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <Select
          label={t('assessments.filters.status')}
          value={filters.status}
          onChange={(event) => updateFilter('status', event.target.value)}
          options={[
            { value: '', label: t('assessments.filters.allStatuses') },
            ...ASSESSMENT_STATUS_VALUES.map((status) => ({
              value: status,
              label: t(`assessmentDetail.enums.status.${status}`),
            })),
          ]}
        />
        <Select
          label={t('assessments.filters.school')}
          value={filters.schoolId}
          onChange={(event) => updateFilter('schoolId', event.target.value)}
          options={[
            { value: '', label: t('assessments.filters.allSchools') },
            ...(schoolOptionsQuery.data?.items ?? []).map((school) => ({
              value: school.id,
              label: school.name,
            })),
          ]}
        />
        <Input
          type="date"
          label={t('assessments.filters.from')}
          value={filters.from}
          onChange={(event) => updateFilter('from', event.target.value)}
        />
        <Input
          type="date"
          label={t('assessments.filters.to')}
          value={filters.to}
          onChange={(event) => updateFilter('to', event.target.value)}
        />
      </div>

      {hasActiveAssessmentsFilters(filters) && (
        <div>
          <Button variant="ghost" size="sm" onClick={clearAll}>
            {t('assessments.filters.clearAll')}
          </Button>
        </div>
      )}
    </div>
  );
}
