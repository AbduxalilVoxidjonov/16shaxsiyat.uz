import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog';
import { useToast } from '@/shared/ui/useToast';
import { useRegenerateSchoolLink } from '../api/useRegenerateSchoolLink';
import type { RegenerateLinkResponse } from '../model/types';

export interface RegenerateLinkDialogProps {
  open: boolean;
  schoolId: string;
  schoolName: string;
  onClose: () => void;
  /** Muvaffaqiyatda — yangi havola/QR darhol ko'rsatilishi uchun (`docs/11` A-3). */
  onSuccess: (result: RegenerateLinkResponse) => void;
}

/**
 * Havolani yangilash — CLAUDE.md "MAXSUS DIQQAT" 1: xavfli amal, har doim tasdiq dialogi
 * bilan, aniq ogohlantirish matni bilan ("Eski havola darhol ishlamay qoladi…").
 */
export function RegenerateLinkDialog({
  open,
  schoolId,
  schoolName,
  onClose,
  onSuccess,
}: RegenerateLinkDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const regenerate = useRegenerateSchoolLink();
  const [error, setError] = useState<string | null>(null);

  function handleClose() {
    setError(null);
    regenerate.reset();
    onClose();
  }

  async function handleConfirm() {
    setError(null);
    try {
      const result = await regenerate.mutateAsync(schoolId);
      toast.show({ variant: 'success', title: t('schools.regenerateDialog.successTitle') });
      onSuccess(result);
      onClose();
    } catch {
      setError(t('schools.regenerateDialog.errorTitle'));
    }
  }

  return (
    <ConfirmDialog
      open={open}
      onClose={handleClose}
      onConfirm={() => void handleConfirm()}
      title={t('schools.regenerateDialog.title')}
      description={schoolName}
      warning={t('schools.regenerateDialog.warning')}
      confirmLabel={t('schools.regenerateDialog.confirmCta')}
      isConfirming={regenerate.isPending}
      error={error ?? undefined}
    />
  );
}
