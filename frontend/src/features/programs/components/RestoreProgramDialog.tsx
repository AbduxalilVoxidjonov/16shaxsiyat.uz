import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { useRestoreProgram } from '../api/useProgramLifecycleMutations';

export interface RestoreProgramDialogProps {
  open: boolean;
  programId: string;
  programName: string;
  onClose: () => void;
}

/**
 * Arxivdan tiklash — tasdiq dialogi bilan (`ArchiveProgramDialog` naqshi, 2026-09-06).
 *
 * Oqibat ANIQ yoziladi: dastur `Paused` ("To'xtatilgan") holatiga qaytadi, `Active` ga EMAS —
 * arxiv maktab biriktirishlarini saqlab qolgan, shu sabab bir bosishda tiklash dasturni o'sha
 * maktablar uchun darhol jonli qilib qo'ygan bo'lardi. Test boshlanishi uchun admin keyin
 * "Faollashtirish"ni alohida bosadi (`AssessmentProgram.Restore` izohi).
 *
 * `ProgramImpactNotice` bu yerda YO'Q: tiklash hech qaysi maktabni havolasiz qoldirmaydi
 * (aksincha), `impact` endpointida ham `restore` amali yo'q.
 *
 * `confirmVariant="primary"`: bu xavfli (destruktiv) amal emas, `ConfirmDialog` ning
 * standart `danger` rangi noto'g'ri signal berardi.
 */
export function RestoreProgramDialog({
  open,
  programId,
  programName,
  onClose,
}: RestoreProgramDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const restoreProgram = useRestoreProgram();
  const [error, setError] = useState<string | null>(null);

  function handleClose() {
    setError(null);
    restoreProgram.reset();
    onClose();
  }

  async function handleConfirm() {
    setError(null);
    try {
      await restoreProgram.mutateAsync(programId);
      toast.show({ variant: 'success', title: t('programs.restoreDialog.successTitle') });
      onClose();
    } catch (caught) {
      setError(
        caught instanceof AppError ? caught.message : t('programs.restoreDialog.genericError'),
      );
    }
  }

  return (
    <ConfirmDialog
      open={open}
      onClose={handleClose}
      onConfirm={() => void handleConfirm()}
      title={t('programs.restoreDialog.title')}
      description={programName}
      warning={t('programs.restoreDialog.description')}
      confirmLabel={t('programs.restoreDialog.confirmCta')}
      confirmVariant="primary"
      isConfirming={restoreProgram.isPending}
      error={error ?? undefined}
    />
  );
}
