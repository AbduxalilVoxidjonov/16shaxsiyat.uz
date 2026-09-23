import { useId } from 'react';
import { useTranslation } from 'react-i18next';
import { Checkbox } from '@/shared/ui/Checkbox';
import { Skeleton } from '@/shared/ui/Skeleton';
import { selectOfferedTests, useTestOptionsQuery } from '../api/useTestOptionsQuery';

export interface SchoolTestsFieldProps {
  value: readonly string[];
  onChange: (next: string[]) => void;
  error?: string;
}

/**
 * Maktab formasidagi "Testlar" ko'p tanlovi (2026-09-23 egasi qarori, `docs/07` §3.1
 * `testIds`). Asosiy biriktirish joyi — test ichidagi "Biriktirish" kartasi; bu yerdagi tanlov
 * qo'shimcha qulaylik. "Barcha maktablarga" ochiq testlar bu ro'yxatda belgilanmaydi — ular
 * baribir har maktabda ko'rinadi.
 */
export function SchoolTestsField({ value, onChange, error }: SchoolTestsFieldProps) {
  const { t } = useTranslation();
  const headingId = useId();
  const testsQuery = useTestOptionsQuery();
  const selected = new Set(value);

  function toggle(testId: string, checked: boolean) {
    onChange(
      checked
        ? [...value.filter((id) => id !== testId), testId]
        : value.filter((id) => id !== testId),
    );
  }

  const offered = testsQuery.data ? selectOfferedTests(testsQuery.data, value) : [];

  return (
    <fieldset
      aria-labelledby={headingId}
      className="flex flex-col gap-1.5"
      data-testid="school-tests-field"
    >
      <legend id={headingId} className="text-sm font-medium text-ink">
        {t('schools.form.testsLabel')}
      </legend>
      <p className="text-xs text-neutral-600">{t('schools.form.testsHint')}</p>

      {testsQuery.isPending ? (
        <div className="flex flex-col gap-2">
          <Skeleton className="h-9 w-full" />
          <Skeleton className="h-9 w-full" />
        </div>
      ) : testsQuery.isError ? (
        <p className="text-sm text-terakota-700" role="alert">
          {t('schools.form.testsLoadError')}
        </p>
      ) : offered.length === 0 ? (
        <p className="text-sm text-neutral-500">{t('schools.form.testsEmpty')}</p>
      ) : (
        <ul className="flex max-h-56 flex-col gap-1 overflow-y-auto rounded-xl border border-line px-3 py-1">
          {offered.map((test) => {
            const statusNote =
              test.status === 'Archived'
                ? t('schools.form.testStatus.archived')
                : test.status === 'Draft'
                  ? t('schools.form.testStatus.draft')
                  : !test.isActive
                    ? t('schools.form.testStatus.inactive')
                    : null;
            return (
              <li key={test.id}>
                <Checkbox
                  label={
                    <>
                      {test.nameUz}{' '}
                      <span className="font-mono text-xs text-neutral-500">{test.code}</span>
                      {statusNote && (
                        <span className="ml-1 text-xs text-zarhal-800">({statusNote})</span>
                      )}
                    </>
                  }
                  checked={selected.has(test.id)}
                  onChange={(event) => toggle(test.id, event.target.checked)}
                />
              </li>
            );
          })}
        </ul>
      )}
      {error && (
        <p className="text-sm text-terakota-700" role="alert">
          {error}
        </p>
      )}
    </fieldset>
  );
}
