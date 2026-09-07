import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog';
import { useToast } from '@/shared/ui/useToast';
import { useRegenerateSchoolEntryCode } from '../api/useRegenerateSchoolEntryCode';
import type { RegenerateEntryCodeResponse } from '../model/types';

export interface RegenerateEntryCodeDialogProps {
  open: boolean;
  schoolId: string;
  schoolName: string;
  onClose: () => void;
  /** Muvaffaqiyatda — yangi kod darhol ko'rsatilishi uchun (`RegenerateLinkDialog` naqshi). */
  onSuccess: (result: RegenerateEntryCodeResponse) => void;
}

/** Maktab kodini qayta yaratish — xavfli amal, har doim tasdiq va aniq ogohlantirish bilan. */
export function RegenerateEntryCodeDialog({
  open,
  schoolId,
  schoolName,
  onClose,
  onSuccess,
}: RegenerateEntryCodeDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const regenerate = useRegenerateSchoolEntryCode();
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
      toast.show({ variant: 'success', title: t('schools.regenerateEntryCodeDialog.successTitle') });
      onSuccess(result);
      onClose();
    } catch {
      setError(t('schools.regenerateEntryCodeDialog.errorTitle'));
    }
  }

  return (
    <ConfirmDialog
      open={open}
      onClose={handleClose}
      onConfirm={() => void handleConfirm()}
      title={t('schools.regenerateEntryCodeDialog.title')}
      description={schoolName}
      warning={t('schools.regenerateEntryCodeDialog.warning')}
      confirmLabel={t('schools.regenerateEntryCodeDialog.confirmCta')}
      isConfirming={regenerate.isPending}
      error={error ?? undefined}
    />
  );
}
