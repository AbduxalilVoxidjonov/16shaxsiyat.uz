import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { usePublishProgram } from '../api/useProgramLifecycleMutations';
import { PROGRAM_DURATION_WARNING_MINUTES, type AdminProgramTestItem } from '../model/types';
import { computeProgramDuration } from '../model/programComputations';
import type { CatalogTestOption } from '../api/useCatalogTestOptionsQuery';

export interface PublishProgramDialogProps {
  open: boolean;
  programId: string;
  programName: string;
  tests: readonly AdminProgramTestItem[];
  catalogOptions: readonly CatalogTestOption[] | undefined;
  onClose: () => void;
}

/**
 * Nashr qilish — xavfli amal (`prompts/35` 10-band + cheklovlar bo'limi). Jami vaqt
 * 40 daqiqadan oshsa sariq ogohlantirish ko'rsatiladi (`prompts/35` 12-band).
 */
export function PublishProgramDialog({
  open,
  programId,
  programName,
  tests,
  catalogOptions,
  onClose,
}: PublishProgramDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const publishProgram = usePublishProgram();
  const [error, setError] = useState<string | null>(null);

  const duration = computeProgramDuration(tests, catalogOptions);
  const showDurationWarning =
    duration.isComplete && duration.totalMinutes > PROGRAM_DURATION_WARNING_MINUTES;

  function handleClose() {
    setError(null);
    publishProgram.reset();
    onClose();
  }

  async function handleConfirm() {
    setError(null);
    try {
      await publishProgram.mutateAsync(programId);
      toast.show({ variant: 'success', title: t('programs.publishDialog.successTitle') });
      onClose();
    } catch (caught) {
      if (caught instanceof AppError && caught.status === 400) {
        setError(t('programs.publishDialog.notPublishableError'));
        return;
      }
      setError(
        caught instanceof AppError ? caught.message : t('programs.publishDialog.genericError'),
      );
    }
  }

  return (
    <ConfirmDialog
      open={open}
      onClose={handleClose}
      onConfirm={() => void handleConfirm()}
      title={t('programs.publishDialog.title')}
      description={programName}
      confirmLabel={t('programs.publishDialog.confirmCta')}
      confirmVariant="primary"
      isConfirming={publishProgram.isPending}
      error={error ?? undefined}
      warning={
        showDurationWarning
          ? t('programs.publishDialog.durationWarning', { minutes: duration.totalMinutes })
          : undefined
      }
    />
  );
}
