import { useEffect, useId, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Search, X } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { useDebounce } from '@/shared/hooks/useDebounce';
import {
  SCHOOL_SEARCH_DEBOUNCE_MS,
  useSchoolNameQuery,
  useSchoolOptionsQuery,
} from '../api/useSchoolOptionsQuery';

export interface SchoolComboboxProps {
  value: string;
  onChange: (schoolId: string, schoolName: string) => void;
  label: string;
}

/**
 * Maktab tanlash — qidiruvli tanlov (CLAUDE.md "MAXSUS DIQQAT" 6: "Maktab tanlash —
 * searchable select, maktablar ko'p bo'lishi mumkin"). Native `<select>` (`shared/ui/Select`)
 * bunda mos emas — barcha maktablarni oldindan yuklash server-side pagination qoidasini
 * buzardi (CLAUDE.md "MAXSUS DIQQAT" 1). Shu sabab har harf terilganda (400ms debounce)
 * `GET /api/admin/schools?search=` so'raladi — xuddi asosiy qidiruv maydoni kabi.
 *
 * `features/schools` importi YO'Q (`features/*` bir-birini import qilmaydi) — shu API'ga
 * `useSchoolOptionsQuery.ts` orqali mustaqil, minimal DTO bilan murojaat qilinadi.
 */
export function SchoolCombobox({ value, onChange, label }: SchoolComboboxProps) {
  const { t } = useTranslation();
  const [isOpen, setIsOpen] = useState(false);
  const [searchInput, setSearchInput] = useState('');
  const containerRef = useRef<HTMLDivElement>(null);
  const listboxId = useId();

  const debouncedSearch = useDebounce(searchInput, SCHOOL_SEARCH_DEBOUNCE_MS);
  const optionsQuery = useSchoolOptionsQuery(debouncedSearch, isOpen);
  const selectedNameQuery = useSchoolNameQuery(value || null);

  useEffect(() => {
    if (!isOpen) return;
    function handlePointerDown(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener('mousedown', handlePointerDown);
    return () => {
      document.removeEventListener('mousedown', handlePointerDown);
    };
  }, [isOpen]);

  const selectedName = value ? (selectedNameQuery.data?.name ?? '') : '';
  const displayValue = isOpen ? searchInput : selectedName;

  function openWithCurrentSelection() {
    setSearchInput(selectedName);
    setIsOpen(true);
  }

  function handleSelect(schoolId: string, schoolName: string) {
    onChange(schoolId, schoolName);
    setIsOpen(false);
  }

  function handleClear() {
    onChange('', '');
    setSearchInput('');
    setIsOpen(false);
  }

  const options = optionsQuery.data?.items ?? [];

  return (
    <div ref={containerRef} className="relative flex w-full flex-col gap-1.5">
      <label htmlFor={`${listboxId}-input`} className="text-sm font-medium text-neutral-700">
        {label}
      </label>
      <div className="relative">
        <Search
          className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-neutral-400"
          aria-hidden="true"
        />
        <input
          id={`${listboxId}-input`}
          role="combobox"
          aria-expanded={isOpen}
          aria-controls={listboxId}
          aria-autocomplete="list"
          autoComplete="off"
          className={cn(
            'h-11 w-full rounded-lg border border-neutral-300 bg-white pr-9 pl-9 text-base text-neutral-900',
            'placeholder:text-neutral-400',
            'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-600',
          )}
          placeholder={t('students.filters.schoolPlaceholder')}
          value={displayValue}
          onFocus={openWithCurrentSelection}
          onChange={(event) => {
            setSearchInput(event.target.value);
            setIsOpen(true);
          }}
          onKeyDown={(event) => {
            if (event.key === 'Escape') {
              setIsOpen(false);
            }
          }}
        />
        {value && !isOpen && (
          <button
            type="button"
            onClick={handleClear}
            aria-label={t('students.filters.schoolClear')}
            className="absolute top-1/2 right-2 -translate-y-1/2 rounded-md p-1 text-neutral-400 hover:bg-neutral-100 hover:text-neutral-700"
          >
            <X size={16} aria-hidden="true" />
          </button>
        )}
      </div>
      {isOpen && (
        <div
          id={listboxId}
          role="listbox"
          aria-label={label}
          className="absolute top-full left-0 z-10 mt-1 max-h-64 w-full overflow-y-auto rounded-lg border border-neutral-200 bg-white py-1 shadow-lg"
        >
          <button
            type="button"
            role="option"
            aria-selected={value === ''}
            onClick={handleClear}
            className="block w-full px-3 py-2 text-left text-sm text-neutral-500 hover:bg-neutral-50"
          >
            {t('students.filters.schoolAll')}
          </button>
          {optionsQuery.isPending && (
            <p className="px-3 py-2 text-sm text-neutral-400">{t('common.loading')}</p>
          )}
          {!optionsQuery.isPending && options.length === 0 && (
            <p className="px-3 py-2 text-sm text-neutral-400">{t('students.filters.schoolNoMatch')}</p>
          )}
          {options.map((option) => (
            <button
              key={option.id}
              type="button"
              role="option"
              aria-selected={option.id === value}
              onClick={() => handleSelect(option.id, option.name)}
              className={cn(
                'block w-full px-3 py-2 text-left text-sm hover:bg-neutral-50',
                option.id === value ? 'bg-primary-50 text-primary-700' : 'text-neutral-900',
              )}
            >
              {option.name}
              <span className="ml-1 text-neutral-400">
                ({option.region}, {option.district})
              </span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
