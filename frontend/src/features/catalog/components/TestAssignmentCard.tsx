import { useId, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Globe, Lock, X } from 'lucide-react';
import { AppError } from '@/shared/api/AppError';
import { useDebounce } from '@/shared/hooks/useDebounce';
import { Badge, type BadgeVariant } from '@/shared/ui/Badge';
import { Button } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';
import { Checkbox } from '@/shared/ui/Checkbox';
import { ErrorState } from '@/shared/ui/ErrorState';
import { Input } from '@/shared/ui/Input';
import { Skeleton } from '@/shared/ui/Skeleton';
import { useToast } from '@/shared/ui/useToast';
import { cn } from '@/shared/lib/cn';
import { useTestAssignmentQuery, useUpdateTestAssignment } from '../api/useTestAssignment';
import {
  SCHOOL_SEARCH_DEBOUNCE_MS,
  useSchoolOptionsQuery,
  useSchoolsByIdsQuery,
  type SchoolOption,
} from '../api/useSchoolOptionsQuery';
import { useCatalogErrorMessage } from '../lib/useCatalogErrorMessage';
import {
  buildAssignmentPayload,
  isAssignmentFormDirty,
  resolveAssignmentStatus,
  toAssignmentFormState,
  type AssignmentAudience,
  type AssignmentStatusKind,
  type TestAssignment,
  type TestAssignmentFormState,
} from '../model/assignmentTypes';

export interface TestAssignmentCardProps {
  testId: string;
}

const STATUS_BADGE_VARIANT: Record<AssignmentStatusKind, BadgeVariant> = {
  active: 'success',
  draft: 'neutral',
  paused: 'warning',
  unassigned: 'neutral',
  archived: 'danger',
  unknown: 'neutral',
};

/**
 * Test ichidagi "Biriktirish" kartasi — `docs/07` §3.4.1, `docs/11` A-8 (2026-09-23 egasi
 * qarori: "Testlar katalogi" va "Dasturlar" bir xil narsa edi, "Dasturlar" olib tashlandi).
 * Eski dastur sahifasidagi biriktirish sozlamalari (ommaviy/biriktirilgan ko'rinish, maktablar,
 * ommaviy makon, ro'yxatdan o'tish rejimi) shu yerga ko'chdi; holat testga ergashadi
 * (`publish`/`toggle-active`/`archive` — sahifa tepasidagi tugmalar).
 */
export function TestAssignmentCard({ testId }: TestAssignmentCardProps) {
  const { t } = useTranslation();
  const query = useTestAssignmentQuery(testId);

  return (
    <Card title={t('catalog.assignment.title')} data-testid="test-assignment-card">
      <p className="-mt-1 mb-4 text-sm text-neutral-600">{t('catalog.assignment.description')}</p>
      {query.isPending ? (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-6 w-64" />
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-16 w-full" />
        </div>
      ) : query.isError ? (
        <ErrorState
          title={t('catalog.assignment.loadErrorTitle')}
          description={t('catalog.assignment.loadErrorDescription')}
          onRetry={() => void query.refetch()}
        />
      ) : (
        <AssignmentEditor key={testId} testId={testId} assignment={query.data} />
      )}
    </Card>
  );
}

/** Ma'lum xato kodlari — kartaga xos, aniq o'zbekcha matn (xom `code` ekranga chiqmaydi). */
const ASSIGNMENT_ERROR_KEYS: Record<string, string> = {
  REGISTRATION_REQUIRED_FOR_BATTERY: 'catalog.assignment.errors.registrationRequiredForBattery',
  PUBLIC_SPACE_NOT_CONFIGURED: 'catalog.assignment.errors.publicSpaceNotConfigured',
  TEST_ARCHIVED: 'catalog.assignment.errors.testArchived',
  VALIDATION_ERROR: 'catalog.assignment.errors.validation',
};

function AssignmentEditor({ testId, assignment }: { testId: string; assignment: TestAssignment }) {
  const { t } = useTranslation();
  const toast = useToast();
  const toCatalogErrorMessage = useCatalogErrorMessage();
  const update = useUpdateTestAssignment();
  const baseId = useId();

  const saved = toAssignmentFormState(assignment);
  const [form, setForm] = useState<TestAssignmentFormState>(saved);
  const [error, setError] = useState<string | null>(null);
  // `409 TEST_ARCHIVED` — test boshqa oynada arxivlangan bo'lishi mumkin: forma darhol
  // faqat o'qish uchun bo'ladi (qayta urinish baribir yiqiladi).
  const [archivedByConflict, setArchivedByConflict] = useState(false);
  // Qidiruvdan tanlangan maktab nomlari — chiplar uchun qo'shimcha so'rovsiz.
  const [knownSchools, setKnownSchools] = useState<Record<string, SchoolOption>>({});

  const isReadOnly = assignment.testStatus === 'Archived' || archivedByConflict;
  const isDirty = isAssignmentFormDirty(form, saved);
  const status = archivedByConflict ? 'archived' : resolveAssignmentStatus(assignment);

  function patch(next: Partial<TestAssignmentFormState>) {
    setError(null);
    setForm((current) => ({ ...current, ...next }));
  }

  function toMessage(caught: unknown): string {
    if (caught instanceof AppError) {
      if (caught.code === 'NOT_FOUND' && Array.isArray(caught.extensions?.schoolIds)) {
        return t('catalog.assignment.errors.schoolsNotFound', {
          count: caught.extensions.schoolIds.length,
        });
      }
      const key = ASSIGNMENT_ERROR_KEYS[caught.code];
      if (key) return t(key);
    }
    return toCatalogErrorMessage(caught);
  }

  async function handleSave() {
    setError(null);
    try {
      const updated = await update.mutateAsync({
        testId,
        payload: buildAssignmentPayload(form),
      });
      setForm(toAssignmentFormState(updated));
      toast.show({ variant: 'success', title: t('catalog.assignment.saveSuccess') });
    } catch (caught) {
      if (caught instanceof AppError && caught.code === 'TEST_ARCHIVED') {
        setArchivedByConflict(true);
      }
      setError(toMessage(caught));
    }
  }

  const audienceName = `${baseId}-audience`;
  const registrationName = `${baseId}-registration`;
  const batteryNoteId = `${baseId}-battery-note`;
  const noneDisabled = isReadOnly || assignment.hasPersonalityBattery;

  return (
    <div className="flex flex-col gap-5">
      {/* Holat ko'rsatkichi — `state`/`isAvailable` backenddan (saqlangan holat bo'yicha). */}
      <div
        className="flex flex-wrap items-center gap-2 rounded-xl bg-neutral-50 px-3 py-2"
        data-testid="assignment-status"
        role="status"
      >
        <Badge variant={STATUS_BADGE_VARIANT[status]}>
          {t(`catalog.assignment.status.${status}.badge`)}
        </Badge>
        <span className="text-sm text-neutral-700">
          {t(`catalog.assignment.status.${status}.text`)}
        </span>
        <span className="text-xs text-neutral-500">
          {t('catalog.assignment.sessionCount', { count: assignment.sessionCount })}
        </span>
      </div>

      {isReadOnly && (
        <p
          className="flex items-start gap-2 rounded-lg bg-terakota-50 p-3 text-sm text-terakota-800"
          data-testid="assignment-readonly-notice"
        >
          <Lock size={16} className="mt-0.5 shrink-0" aria-hidden="true" />
          {t('catalog.assignment.readOnlyNotice')}
        </p>
      )}

      <fieldset disabled={isReadOnly} className="flex flex-col gap-5">
        {/* Kimga ochiq */}
        <fieldset className="flex flex-col gap-2">
          <legend className="mb-1 text-sm font-semibold text-ink">
            {t('catalog.assignment.audience.legend')}
          </legend>
          <AudienceRadio
            name={audienceName}
            value="all"
            checked={form.audience === 'all'}
            label={t('catalog.assignment.audience.all')}
            hint={t('catalog.assignment.audience.allHint')}
            onSelect={() => patch({ audience: 'all' })}
          />
          <AudienceRadio
            name={audienceName}
            value="selected"
            checked={form.audience === 'selected'}
            label={t('catalog.assignment.audience.selected')}
            hint={t('catalog.assignment.audience.selectedHint')}
            onSelect={() => patch({ audience: 'selected' })}
          />
          {form.audience === 'selected' && (
            <SchoolMultiSelect
              selectedIds={form.schoolIds}
              knownSchools={knownSchools}
              disabled={isReadOnly}
              onChange={(schoolIds) => patch({ schoolIds })}
              onRemember={(school) =>
                setKnownSchools((current) => ({ ...current, [school.id]: school }))
              }
            />
          )}
        </fieldset>

        {/* Ommaviy makon — maktabsiz, Telegram kabineti (`/kirish`) */}
        <div className="rounded-xl border border-firuza-200 bg-firuza-50 px-3 py-2">
          <div className="flex items-start gap-2">
            <Globe size={18} className="mt-2 shrink-0 text-firuza-700" aria-hidden="true" />
            <div className="min-w-0 flex-1">
              <Checkbox
                label={t('catalog.assignment.publicSpace.label')}
                checked={form.audience === 'all' ? true : form.isInPublicSpace}
                disabled={isReadOnly || form.audience === 'all'}
                onChange={(event) => patch({ isInPublicSpace: event.target.checked })}
              />
              <p className="text-xs text-neutral-600">
                {form.audience === 'all'
                  ? t('catalog.assignment.publicSpace.impliedByAll')
                  : t('catalog.assignment.publicSpace.hint')}
              </p>
            </div>
          </div>
        </div>

        {/* Ro'yxatdan o'tish rejimi */}
        <fieldset className="flex flex-col gap-2">
          <legend className="mb-1 text-sm font-semibold text-ink">
            {t('catalog.assignment.registration.legend')}
          </legend>
          <AudienceRadio
            name={registrationName}
            value="Full"
            checked={form.registrationMode === 'Full'}
            label={t('catalog.assignment.registration.full')}
            hint={t('catalog.assignment.registration.fullHint')}
            onSelect={() => patch({ registrationMode: 'Full' })}
          />
          <AudienceRadio
            name={registrationName}
            value="None"
            checked={form.registrationMode === 'None'}
            label={t('catalog.assignment.registration.none')}
            hint={t('catalog.assignment.registration.noneHint')}
            disabled={noneDisabled}
            describedBy={assignment.hasPersonalityBattery ? batteryNoteId : undefined}
            onSelect={() => patch({ registrationMode: 'None' })}
          />
          {assignment.hasPersonalityBattery && (
            <p id={batteryNoteId} className="text-xs font-medium text-zarhal-800" role="note">
              {t('catalog.assignment.registration.batteryNote')}
            </p>
          )}
        </fieldset>
      </fieldset>

      {error && (
        <p
          role="alert"
          className="rounded-lg bg-terakota-50 p-3 text-sm text-terakota-800"
          data-testid="assignment-error"
        >
          {error}
        </p>
      )}

      {!isReadOnly && (
        <div className="flex flex-wrap items-center justify-end gap-2">
          {isDirty && (
            <Button
              variant="outline"
              onClick={() => {
                setError(null);
                setForm(saved);
              }}
              disabled={update.isPending}
            >
              {t('catalog.assignment.resetCta')}
            </Button>
          )}
          <Button
            onClick={() => void handleSave()}
            isLoading={update.isPending}
            disabled={!isDirty}
          >
            {t('catalog.assignment.saveCta')}
          </Button>
        </div>
      )}
    </div>
  );
}

function AudienceRadio({
  name,
  value,
  checked,
  label,
  hint,
  disabled,
  describedBy,
  onSelect,
}: {
  name: string;
  value: AssignmentAudience | 'Full' | 'None';
  checked: boolean;
  label: string;
  hint: string;
  disabled?: boolean;
  describedBy?: string;
  onSelect: () => void;
}) {
  const id = useId();
  return (
    <label
      htmlFor={id}
      className={cn(
        'flex min-h-11 cursor-pointer items-start gap-3 rounded-xl border px-3 py-2',
        checked ? 'border-firuza-400 bg-firuza-50' : 'border-line',
        disabled && 'cursor-not-allowed opacity-60',
      )}
    >
      <input
        id={id}
        type="radio"
        name={name}
        value={value}
        checked={checked}
        disabled={disabled}
        aria-describedby={describedBy}
        onChange={onSelect}
        className="mt-1 size-4 shrink-0 text-firuza-600"
      />
      <span className="text-sm font-medium text-ink">
        {label}
        <span className="block text-xs font-normal text-neutral-600">{hint}</span>
      </span>
    </label>
  );
}

/**
 * Qidiruvli ko'p tanlov: tanlanganlar chip sifatida (× bilan olib tashlanadi), pastda qidiruv
 * natijalari checkbox bilan. Tanlov faqat forma holatini o'zgartiradi — saqlash "Saqlash"
 * tugmasi bilan (bitta `PUT`, to'plam to'liq almashtiriladi).
 */
function SchoolMultiSelect({
  selectedIds,
  knownSchools,
  disabled,
  onChange,
  onRemember,
}: {
  selectedIds: readonly string[];
  knownSchools: Record<string, SchoolOption>;
  disabled: boolean;
  onChange: (ids: string[]) => void;
  onRemember: (school: SchoolOption) => void;
}) {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebounce(search.trim(), SCHOOL_SEARCH_DEBOUNCE_MS);
  const optionsQuery = useSchoolOptionsQuery(debouncedSearch, !disabled);

  // Nomi hali ma'lum bo'lmagan (serverdan kelgan) ID'lar uchungina qo'shimcha so'rov.
  const unknownIds = selectedIds.filter((id) => !knownSchools[id]);
  const namesQuery = useSchoolsByIdsQuery(unknownIds);
  const nameById = new Map<string, string>();
  for (const school of namesQuery.data ?? []) nameById.set(school.id, school.name);
  for (const school of Object.values(knownSchools)) nameById.set(school.id, school.name);

  const selectedSet = new Set(selectedIds);

  function toggle(school: SchoolOption, checked: boolean) {
    onRemember(school);
    onChange(
      checked
        ? [...selectedIds.filter((id) => id !== school.id), school.id]
        : selectedIds.filter((id) => id !== school.id),
    );
  }

  return (
    <div className="ml-7 flex flex-col gap-3" data-testid="assignment-school-picker">
      <div>
        <p className="mb-1 text-xs font-medium text-neutral-600">
          {t('catalog.assignment.schools.selectedHeading', { count: selectedIds.length })}
        </p>
        {selectedIds.length === 0 ? (
          <p className="text-sm text-neutral-500">{t('catalog.assignment.schools.noneSelected')}</p>
        ) : (
          <ul className="flex flex-wrap gap-2">
            {selectedIds.map((id) => {
              const name =
                nameById.get(id) ??
                (namesQuery.isPending ? '…' : t('catalog.assignment.schools.unknownSchool'));
              return (
                <li
                  key={id}
                  className="flex items-center gap-1 rounded-full bg-primary-50 py-1 pr-1 pl-3 text-sm text-primary-700"
                >
                  {name}
                  <button
                    type="button"
                    className="rounded-full p-1 hover:bg-primary-100"
                    aria-label={t('catalog.assignment.schools.removeAria', { name })}
                    onClick={() => onChange(selectedIds.filter((selected) => selected !== id))}
                  >
                    <X size={14} aria-hidden="true" />
                  </button>
                </li>
              );
            })}
          </ul>
        )}
      </div>

      <Input
        label={t('catalog.assignment.schools.searchLabel')}
        placeholder={t('catalog.assignment.schools.searchPlaceholder')}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
      />

      {optionsQuery.isPending ? (
        <div className="flex flex-col gap-2">
          {Array.from({ length: 3 }, (_, index) => (
            <Skeleton key={index} className="h-10 w-full" />
          ))}
        </div>
      ) : optionsQuery.isError ? (
        <p className="text-sm text-terakota-700" role="alert">
          {t('catalog.assignment.schools.loadError')}
        </p>
      ) : (optionsQuery.data?.items.length ?? 0) === 0 ? (
        <p className="text-sm text-neutral-500">{t('catalog.assignment.schools.noResults')}</p>
      ) : (
        <ul className="flex max-h-64 flex-col gap-1 overflow-y-auto">
          {optionsQuery.data?.items.map((school) => (
            <li key={school.id} className="rounded-lg border border-neutral-200 px-3">
              <Checkbox
                label={
                  <>
                    <span className="block text-ink">{school.name}</span>
                    <span className="block text-xs text-neutral-500">
                      {school.region}, {school.district}
                    </span>
                  </>
                }
                checked={selectedSet.has(school.id)}
                onChange={(event) => toggle(school, event.target.checked)}
              />
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
