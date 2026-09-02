import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Dialog } from '@/shared/ui/Dialog';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { Badge } from '@/shared/ui/Badge';
import { Skeleton } from '@/shared/ui/Skeleton';
import { ErrorState } from '@/shared/ui/ErrorState';
import { EmptyState } from '@/shared/ui/EmptyState';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { useCatalogTestOptionsQuery } from '../api/useCatalogTestOptionsQuery';
import { useAddProgramTest } from '../api/useProgramTestMutations';
import type { AdminProgramTestItem } from '../model/types';

export interface AddTestDialogProps {
  open: boolean;
  programId: string;
  existingTests: readonly AdminProgramTestItem[];
  onClose: () => void;
}

/**
 * Dasturga test qo'shish paneli — `prompts/35` C8-band ("testlarni tanlash"). Katalog
 * ro'yxati (`useCatalogTestOptionsQuery`) hozircha backend'da yo'q endpointga murojaat
 * qiladi (kod izohiga qarang) — shu sabab bu yerda `ErrorState` ko'rsatilishi kutilgan,
 * xato emas: qo'shiladigan test yo'q, chunki ularni ro'yxatlaydigan API hali yozilmagan.
 */
export function AddTestDialog({ open, programId, existingTests, onClose }: AddTestDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const [search, setSearch] = useState('');

  const optionsQuery = useCatalogTestOptionsQuery(open);
  const addTest = useAddProgramTest();

  const existingIds = useMemo(
    () => new Set(existingTests.map((test) => test.testDefinitionId)),
    [existingTests],
  );

  const availableOptions = (optionsQuery.data ?? [])
    .filter((option) => !existingIds.has(option.id))
    .filter((option) => {
      const term = search.trim().toLowerCase();
      if (!term) return true;
      return option.nameUz.toLowerCase().includes(term) || option.code.toLowerCase().includes(term);
    });

  async function handleAdd(testDefinitionId: string) {
    try {
      const nextOrder = existingTests.length + 1;
      await addTest.mutateAsync({
        programId,
        payload: { testDefinitionId, displayOrder: nextOrder },
      });
      toast.show({ variant: 'success', title: t('programs.addTestDialog.successTitle') });
    } catch (caught) {
      const message =
        caught instanceof AppError && caught.code === 'SYSTEM_PROGRAM_LOCKED'
          ? t('programs.errors.systemProgramLocked')
          : caught instanceof AppError
            ? caught.message
            : t('programs.addTestDialog.genericError');
      toast.show({ variant: 'danger', title: message });
    }
  }

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('programs.addTestDialog.title')}
      footer={
        <Button variant="outline" onClick={onClose}>
          {t('common.close')}
        </Button>
      }
    >
      <div className="flex flex-col gap-3">
        <Input
          label={t('programs.addTestDialog.searchLabel')}
          placeholder={t('programs.addTestDialog.searchPlaceholder')}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
        />

        {optionsQuery.isPending && (
          <div className="flex flex-col gap-2">
            {Array.from({ length: 3 }, (_, index) => (
              <Skeleton key={index} className="h-14 w-full" />
            ))}
          </div>
        )}

        {optionsQuery.isError && (
          <ErrorState
            title={t('programs.addTestDialog.catalogUnavailableTitle')}
            description={t('programs.addTestDialog.catalogUnavailableDescription')}
            onRetry={() => void optionsQuery.refetch()}
          />
        )}

        {!optionsQuery.isPending && !optionsQuery.isError && availableOptions.length === 0 && (
          <EmptyState
            title={t('programs.addTestDialog.emptyTitle')}
            description={t('programs.addTestDialog.emptyDescription')}
          />
        )}

        {!optionsQuery.isPending && !optionsQuery.isError && availableOptions.length > 0 && (
          <ul className="flex flex-col gap-2">
            {availableOptions.map((option) => (
              <li
                key={option.id}
                className="flex items-center justify-between gap-3 rounded-lg border border-neutral-200 p-3"
              >
                <div>
                  <p className="text-sm font-medium text-neutral-900">{option.nameUz}</p>
                  <p className="text-xs text-neutral-500">
                    {option.code} ·{' '}
                    {t('programs.addTestDialog.questionsAndMinutes', {
                      questions: option.questionCount,
                      minutes: option.estimatedMinutes,
                    })}
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  {option.isSystem && <Badge variant="neutral">{t('catalog.badge.system')}</Badge>}
                  <Button
                    size="sm"
                    variant="outline"
                    isLoading={addTest.isPending}
                    onClick={() => void handleAdd(option.id)}
                  >
                    {t('programs.addTestDialog.addCta')}
                  </Button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </Dialog>
  );
}
