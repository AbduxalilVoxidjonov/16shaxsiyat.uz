import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { useArchiveProgram } from '../api/useProgramLifecycleMutations';
import { useProgramImpactQuery } from '../api/useProgramImpactQuery';
import { ProgramImpactNotice } from './ProgramImpactNotice';

export interface ArchiveProgramDialogProps {
  open: boolean;
  programId: string;
  programName: string;
  onClose: () => void;
}

/**
 * Arxivlash — xavfli amal, tasdiq dialogi bilan (`prompts/35` cheklovlar bo'limi).
 *
 * **2026-09-03:** tasdiq oynasida amal NECHTA maktabni havolasiz qoldirishi ham ko'rsatiladi
 * (`GET /api/admin/programs/{id}/impact?action=archive`). Amal taqiqlanmaydi — faqat oqibat.
 */
export function ArchiveProgramDialog({
  open,
  programId,
  programName,
  onClose,
}: ArchiveProgramDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const archiveProgram = useArchiveProgram();
  const impactQuery = useProgramImpactQuery(programId, 'archive', open);
  const [error, setError] = useState<string | null>(null);

  function handleClose() {
    setError(null);
    archiveProgram.reset();
    onClose();
  }

  async function handleConfirm() {
    setError(null);
    try {
      await archiveProgram.mutateAsync(programId);
      toast.show({ variant: 'success', title: t('programs.archiveDialog.successTitle') });
      onClose();
    } catch (caught) {
      setError(
        caught instanceof AppError ? caught.message : t('programs.archiveDialog.genericError'),
      );
    }
  }

  return (
    <ConfirmDialog
      open={open}
      onClose={handleClose}
      onConfirm={() => void handleConfirm()}
      title={t('programs.archiveDialog.title')}
      description={programName}
      warning={t('programs.archiveDialog.description')}
      confirmLabel={t('programs.archiveDialog.confirmCta')}
      isConfirming={archiveProgram.isPending}
      error={error ?? undefined}
    >
      <ProgramImpactNotice
        impact={impactQuery.data}
        isPending={impactQuery.isPending}
        isError={impactQuery.isError}
      />
    </ConfirmDialog>
  );
}
