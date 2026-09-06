import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { useToggleProgramActive } from '../api/useProgramLifecycleMutations';
import { useProgramImpactQuery } from '../api/useProgramImpactQuery';
import { ProgramImpactNotice } from './ProgramImpactNotice';

export interface DeactivateProgramDialogProps {
  open: boolean;
  programId: string;
  programName: string;
  onClose: () => void;
}

/**
 * Dasturni TO'XTATISH tasdig'i (`Active ──▶ Paused`) — 2026-09-03 jonli hodisasi:
 * `toggle-active` ilgari BIRDANIGA, tasdiqsiz ishlardi va yagona dastur o'chirilganda
 * barcha maktab havolasi jimgina o'lik bo'lib qolgan edi.
 *
 * Amal TAQIQLANMAYDI — faqat nechta maktab dastursiz qolishi ko'rsatiladi
 * (`GET /api/admin/programs/{id}/impact?action=deactivate`). Dasturni QAYTA
 * FAOLLASHTIRISHDA bu dialog umuman ochilmaydi (zararsiz amal).
 *
 * **Nom haqida:** komponent, endpoint (`toggle-active`) va `impact` amali (`deactivate`)
 * API atamasini saqlaydi; foydalanuvchiga ko'rinadigan matn esa YAGONA holat atamasi bilan
 * ("To'xtatish" ──▶ "To'xtatilgan") yozilgan, `docs/04` "Dastur holati" jadvali bo'yicha.
 * Dialog faqat `Active` holatda ochiladi — `ProgramDetailPage` boshqa holatda bu tugmani
 * umuman ko'rsatmaydi (domen ham `409 PROGRAM_INVALID_TRANSITION` bilan rad etadi).
 */
export function DeactivateProgramDialog({
  open,
  programId,
  programName,
  onClose,
}: DeactivateProgramDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const toggleActive = useToggleProgramActive();
  const impactQuery = useProgramImpactQuery(programId, 'deactivate', open);
  const [error, setError] = useState<string | null>(null);

  function handleClose() {
    setError(null);
    toggleActive.reset();
    onClose();
  }

  async function handleConfirm() {
    setError(null);
    try {
      await toggleActive.mutateAsync(programId);
      toast.show({ variant: 'success', title: t('programs.toggleActive.success') });
      onClose();
    } catch (caught) {
      setError(
        caught instanceof AppError ? caught.message : t('programs.deactivateDialog.genericError'),
      );
    }
  }

  return (
    <ConfirmDialog
      open={open}
      onClose={handleClose}
      onConfirm={() => void handleConfirm()}
      title={t('programs.deactivateDialog.title')}
      description={programName}
      warning={t('programs.deactivateDialog.description')}
      confirmLabel={t('programs.deactivateDialog.confirmCta')}
      isConfirming={toggleActive.isPending}
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
