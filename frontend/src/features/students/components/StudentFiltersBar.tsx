import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router';
import { useTranslation } from 'react-i18next';
import { Input } from '@/shared/ui/Input';
import { Select, type SelectOption } from '@/shared/ui/Select';
import { Checkbox } from '@/shared/ui/Checkbox';
import { useDebounce } from '@/shared/hooks/useDebounce';
import {
  ACTIVITY_LEVEL_VALUES,
  ASSESSMENT_STATUS_VALUES,
  GENDER_FILTER_VALUES,
  PERSONALITY_TYPE_CODES,
} from '../model/enums';
import {
  AGE_RANGE_PRESETS,
  STUDENTS_FILTER_KEYS,
  ageRangeOptionValue,
  formatAgeRange,
  hasActiveStudentsFilters,
  parseAgeRangeOptionValue,
  readStudentsFilters,
  type AgeRange,
  type StudentsChipKey,
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
 * sana oralig'i, qidiruv" + jins va yosh (egasining talabi, 2026-09-07). Barcha holat URL
 * query'da (`useSearchParams`, CLAUDE.md "MAXSUS DIQQAT" 2) — `SchoolFiltersBar.tsx` (P23) va
 * `PublicSpaceUsersSection.tsx` bilan bir xil naqsh.
 *
 * Bu bo'limda FAQAT maktab o'quvchilari — ommaviy makon foydalanuvchilari `/admin/ommaviy` da,
 * shu sabab "Manba" filtri yo'q (backend `?source=` ni endi qabul qilmaydi).
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

  function updateFilter(key: Exclude<StudentsFilterKey, 'search' | 'ageMin' | 'ageMax'>, value: string) {
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

  /** Yosh — IKKI URL parametri birga yoziladi/o'chadi (`ageMin`/`ageMax`), `docs/07` 3.2. */
  function updateAgeRange(range: AgeRange | null) {
    setSearchParams((prev) => {
      const params = new URLSearchParams(prev);
      params.delete('ageMin');
      params.delete('ageMax');
      if (range?.ageMin !== undefined) params.set('ageMin', String(range.ageMin));
      if (range?.ageMax !== undefined) params.set('ageMax', String(range.ageMax));
      params.set('page', '1');
      return params;
    });
  }

  function handleRemoveFilter(key: StudentsChipKey) {
    if (key === 'search') {
      setSearchInput('');
    }
    setSearchParams((prev) => {
      const params = new URLSearchParams(prev);
      if (key === 'age') {
        params.delete('ageMin');
        params.delete('ageMax');
      } else {
        params.delete(key);
      }
      params.set('page', '1');
      return params;
    });
  }

  function handleClearAll() {
    setSearchInput('');
    setSearchParams((prev) => {
      const params = new URLSearchParams(prev);
      for (const key of STUDENTS_FILTER_KEYS) {
        params.delete(key);
      }
      params.set('page', '1');
      return params;
    });
  }

  const schoolNameQuery = useSchoolNameQuery(filters.schoolId || null);

  // Tayyor oraliqlar + (chuqur havoladan kelgan) ixtiyoriy oraliq, agar u tayyorlardan biri
  // bo'lmasa — `Select` hech qachon "bo'sh" ko'rinib, aslida filtr faol bo'lib qolmaydi.
  const currentAgeValue = filters.age ? ageRangeOptionValue(filters.age) : '';
  const ageOptions: SelectOption[] = [
    { value: '', label: t('students.filters.ageAll') },
    ...AGE_RANGE_PRESETS.map((range) => ({
      value: ageRangeOptionValue(range),
      label: formatAgeRange(range, t),
    })),
  ];
  if (filters.age && !ageOptions.some((option) => option.value === currentAgeValue)) {
    ageOptions.push({ value: currentAgeValue, label: formatAgeRange(filters.age, t) });
  }

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

        <div className="w-full sm:w-36">
          <Select
            label={t('students.filters.genderLabel')}
            value={filters.gender}
            onChange={(event) => updateFilter('gender', event.target.value)}
            options={[
              { value: '', label: t('students.filters.genderAll') },
              ...GENDER_FILTER_VALUES.map((gender) => ({
                value: gender,
                label: t(`students.enums.gender.${gender}`),
              })),
            ]}
          />
        </div>

        <div className="w-full sm:w-40">
          <Select
            label={t('students.filters.ageLabel')}
            value={currentAgeValue}
            onChange={(event) => updateAgeRange(parseAgeRangeOptionValue(event.target.value))}
            options={ageOptions}
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
