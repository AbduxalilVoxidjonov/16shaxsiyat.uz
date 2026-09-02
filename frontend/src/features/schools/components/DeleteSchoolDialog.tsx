import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { useDeleteSchool } from '../api/useDeleteSchool';
import { SCHOOL_ERROR_CODES } from '../model/types';

export interface DeleteSchoolDialogProps {
  open: boolean;
  schoolId: string;
  schoolName: string;
  onClose: () => void;
}

/**
 * Maktabni o'chirish — xavfli amal, tasdiq dialogi bilan. `409` (o'quvchisi bor) holatida
 * CLAUDE.md "MAXSUS DIQQAT" 5-band talab qilgan aniq xabar ko'rsatiladi, umumiy xato emas.
 */
export function DeleteSchoolDialog({ open, schoolId, schoolName, onClose }: DeleteSchoolDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const deleteSchool = useDeleteSchool();
  const [error, setError] = useState<string | null>(null);

  function handleClose() {
    setError(null);
    deleteSchool.reset();
    onClose();
  }

  async function handleConfirm() {
    setError(null);
    try {
      await deleteSchool.mutateAsync(schoolId);
      toast.show({ variant: 'success', title: t('schools.deleteDialog.successTitle') });
      onClose();
    } catch (caught) {
      if (
        caught instanceof AppError &&
        (caught.status === 409 || caught.code === SCHOOL_ERROR_CODES.hasStudents)
      ) {
        setError(t('schools.deleteDialog.hasStudentsError'));
        return;
      }
      setError(t('schools.deleteDialog.genericError'));
    }
  }

  return (
    <ConfirmDialog
      open={open}
      onClose={handleClose}
      onConfirm={() => void handleConfirm()}
      title={t('schools.deleteDialog.title')}
      description={schoolName}
      warning={t('schools.deleteDialog.description')}
      confirmLabel={t('schools.deleteDialog.confirmCta')}
      isConfirming={deleteSchool.isPending}
      error={error ?? undefined}
    />
  );
}
