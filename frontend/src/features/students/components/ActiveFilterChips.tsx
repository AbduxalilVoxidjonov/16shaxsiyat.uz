import { X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/ui/Button';
import {
  formatAgeRange,
  type StudentsChipKey,
  type StudentsFilterValues,
} from '../model/studentsFilters';

export interface ActiveFilterChip {
  key: StudentsChipKey;
  label: string;
}

export interface ActiveFilterChipsProps {
  filters: StudentsFilterValues;
  /** `schoolId` tanlangan bo'lsa uning nomi (chip matni uchun) — kombobox allaqachon oladi. */
  schoolName: string;
  onRemove: (key: StudentsChipKey) => void;
  onClearAll: () => void;
}

/**
 * Faol filtrlar chipi — CLAUDE.md "MAXSUS DIQQAT" 5: har birini alohida olib tashlash
 * imkoniyati + "Hammasini tozalash". Chip olib tashlanganda `onRemove` chaqiruvchida
 * (`StudentFiltersBar.tsx`) URL query'ni yangilaydi.
 */
export function ActiveFilterChips({ filters, schoolName, onRemove, onClearAll }: ActiveFilterChipsProps) {
  const { t } = useTranslation();

  const chips: ActiveFilterChip[] = [];
  if (filters.search) {
    chips.push({ key: 'search', label: t('students.chips.search', { value: filters.search }) });
  }
  if (filters.schoolId) {
    chips.push({
      key: 'schoolId',
      label: t('students.chips.school', { value: schoolName || filters.schoolId }),
    });
  }
  if (filters.grade) {
    chips.push({ key: 'grade', label: t('students.chips.grade', { value: filters.grade }) });
  }
  if (filters.status) {
    chips.push({
      key: 'status',
      label: t('students.chips.status', { value: t(`students.enums.status.${filters.status}`) }),
    });
  }
  if (filters.personalityType) {
    chips.push({
      key: 'personalityType',
      label: t('students.chips.personalityType', { value: filters.personalityType }),
    });
  }
  if (filters.activityLevel) {
    chips.push({
      key: 'activityLevel',
      label: t('students.chips.activityLevel', {
        value: t(`students.enums.activityLevel.${filters.activityLevel}`),
      }),
    });
  }
  if (filters.gender) {
    chips.push({
      key: 'gender',
      label: t('students.chips.gender', { value: t(`students.enums.gender.${filters.gender}`) }),
    });
  }
  if (filters.age) {
    chips.push({ key: 'age', label: t('students.chips.age', { value: formatAgeRange(filters.age, t) }) });
  }
  if (filters.needsAttention) {
    chips.push({ key: 'needsAttention', label: t('students.chips.needsAttention') });
  }
  if (filters.from) {
    chips.push({ key: 'from', label: t('students.chips.from', { value: filters.from }) });
  }
  if (filters.to) {
    chips.push({ key: 'to', label: t('students.chips.to', { value: filters.to }) });
  }

  if (chips.length === 0) return null;

  return (
    <div className="flex flex-wrap items-center gap-2" aria-label={t('students.chips.ariaLabel')}>
      {chips.map((chip) => (
        <span
          key={chip.key}
          className="inline-flex items-center gap-1 rounded-full bg-primary-50 py-1 pr-1 pl-3 text-sm text-primary-700"
        >
          {chip.label}
          <button
            type="button"
            onClick={() => {
              onRemove(chip.key);
            }}
            aria-label={t('students.chips.remove', { value: chip.label })}
            className="rounded-full p-1 hover:bg-primary-100"
          >
            <X size={12} aria-hidden="true" />
          </button>
        </span>
      ))}
      <Button variant="ghost" size="sm" onClick={onClearAll}>
        {t('students.chips.clearAll')}
      </Button>
    </div>
  );
}
