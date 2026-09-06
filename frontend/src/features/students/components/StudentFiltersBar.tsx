import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { Input } from '@/shared/ui/Input';
import { Select } from '@/shared/ui/Select';
import { Checkbox } from '@/shared/ui/Checkbox';
import { useDebounce } from '@/shared/hooks/useDebounce';
import {
  ACTIVITY_LEVEL_VALUES,
  ASSESSMENT_STATUS_VALUES,
  PERSONALITY_TYPE_CODES,
  STUDENT_SOURCE_QUERY_VALUES,
} from '../model/enums';
import {
  hasActiveStudentsFilters,
  readStudentsFilters,
  type StudentsFilterKey,
} from '../model/studentsFilters';
import { useSchoolNameQuery } from '../api/useSchoolOptionsQuery';
import { SchoolCombobox } from './SchoolCombobox';
import { ActiveFilterChips } from './ActiveFilterChips';

const SEARCH_DEBOUNCE_MS = 400;
const GRADES = Array.from({ length: 11 }, (_, index) => index + 1);

/**
 * O'quvchilar filtr paneli — docs/11-ux-va-ekranlar.md A-4: "maktab (searchable select),
 * sinf, holat, shaxsiyat tipi, aktivlik darajasi, 'faqat e'tibor talab qiladiganlar' toggle,
 * sana oralig'i, qidiruv". Barcha holat URL query'da (`useSearchParams`, CLAUDE.md "MAXSUS
 * DIQQAT" 2) — `SchoolFiltersBar.tsx` (P23) bilan bir xil naqsh.
 */
export function StudentFiltersBar() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();
  const filters = readStudentsFilters(searchParams);

  const [searchInput, setSearchInput] = useState(filters.search);
  // Boshqa joydan (masalan chipni olib tashlash, orqaga tugmasi) URL o'zgarsa, matn
  // maydoni ham sinxronlansin — effekt EMAS, render vaqtida moslashtirish (`SchoolFiltersBar`
  // bilan bir xil naqsh, P23 hisobotidagi izohga qarang).
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

  function updateFilter(key: Exclude<StudentsFilterKey, 'search'>, value: string) {
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

  function handleRemoveFilter(key: StudentsFilterKey) {
    if (key === 'search') {
      setSearchInput('');
    }
    setSearchParams((prev) => {
      const params = new URLSearchParams(prev);
      params.delete(key);
      params.set('page', '1');
      return params;
    });
  }

  function handleClearAll() {
    setSearchInput('');
    setSearchParams((prev) => {
      const params = new URLSearchParams(prev);
      for (const key of [
        'search',
        'source',
        'schoolId',
        'grade',
        'status',
        'personalityType',
        'activityLevel',
        'needsAttention',
        'from',
        'to',
      ]) {
        params.delete(key);
      }
      params.set('page', '1');
      return params;
    });
  }

  const schoolNameQuery = useSchoolNameQuery(filters.schoolId || null);

  return (
    <div className="flex flex-col gap-3">
      <div
        data-testid="students-filters"
        className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-end"
      >
        <div className="w-full sm:max-w-xs">
          <Input
            label={t('students.filters.searchLabel')}
            placeholder={t('students.filters.searchPlaceholder')}
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
          />
        </div>

        {/*
          Manba filtri (P48) — maktab kombobox'idan OLDIN turadi: avval "qaysi oqim", keyin
          "qaysi maktab". Ommaviy makon endi maktablar ro'yxatida yo'q, shu sabab ommaviy
          foydalanuvchilarga tushishning YAGONA yo'li shu filtr.
        */}
        <div className="w-full sm:w-44">
          <Select
            label={t('students.filters.sourceLabel')}
            placeholder={t('students.filters.sourceAll')}
            value={filters.source}
            onChange={(event) => updateFilter('source', event.target.value)}
            options={STUDENT_SOURCE_QUERY_VALUES.map((value) => ({
              value,
              label:
                value === 'school'
                  ? t('students.filters.sourceSchool')
                  : t('students.filters.sourcePublic'),
            }))}
          />
        </div>

        <div className="w-full sm:w-64">
          <SchoolCombobox
            label={t('students.filters.schoolLabel')}
            value={filters.schoolId}
            onChange={(schoolId) => updateFilter('schoolId', schoolId)}
          />
        </div>

        <div className="w-full sm:w-32">
          <Select
            label={t('students.filters.gradeLabel')}
            placeholder={t('students.filters.gradeAll')}
            value={filters.grade}
            onChange={(event) => updateFilter('grade', event.target.value)}
            options={GRADES.map((grade) => ({
              value: String(grade),
              label: t('students.filters.gradeOption', { grade }),
            }))}
          />
        </div>

        <div className="w-full sm:w-48">
          <Select
            label={t('students.filters.statusLabel')}
            placeholder={t('students.filters.statusAll')}
            value={filters.status}
            onChange={(event) => updateFilter('status', event.target.value)}
            options={ASSESSMENT_STATUS_VALUES.map((status) => ({
              value: status,
              label: t(`students.enums.status.${status}`),
            }))}
          />
        </div>

        <div className="w-full sm:w-40">
          <Select
            label={t('students.filters.personalityTypeLabel')}
            placeholder={t('students.filters.personalityTypeAll')}
            value={filters.personalityType}
            onChange={(event) => updateFilter('personalityType', event.target.value)}
            options={PERSONALITY_TYPE_CODES.map((code) => ({ value: code, label: code }))}
          />
        </div>

        <div className="w-full sm:w-48">
          <Select
            label={t('students.filters.activityLevelLabel')}
            placeholder={t('students.filters.activityLevelAll')}
            value={filters.activityLevel}
            onChange={(event) => updateFilter('activityLevel', event.target.value)}
            options={ACTIVITY_LEVEL_VALUES.map((level) => ({
              value: level,
              label: t(`students.enums.activityLevel.${level}`),
            }))}
          />
        </div>

        <div className="flex w-full items-center sm:w-auto">
          <Checkbox
            label={t('students.filters.needsAttentionLabel')}
            checked={filters.needsAttention}
            onChange={(event) => updateFilter('needsAttention', event.target.checked ? 'true' : '')}
          />
        </div>

        <div className="w-full sm:w-40">
          <Input
            type="date"
            label={t('students.filters.fromLabel')}
            value={filters.from}
            onChange={(event) => updateFilter('from', event.target.value)}
          />
        </div>

        <div className="w-full sm:w-40">
          <Input
            type="date"
            label={t('students.filters.toLabel')}
            value={filters.to}
            onChange={(event) => updateFilter('to', event.target.value)}
          />
        </div>
      </div>

      {hasActiveStudentsFilters(filters) && (
        <ActiveFilterChips
          filters={filters}
          schoolName={schoolNameQuery.data?.name ?? ''}
          onRemove={handleRemoveFilter}
          onClearAll={handleClearAll}
        />
      )}
    </div>
  );
}
