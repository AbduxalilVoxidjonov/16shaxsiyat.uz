import { useTranslation } from 'react-i18next';
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog';

export interface DeleteAccountDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: () => void;
  isDeleting?: boolean;
  error?: string;
}

/**
 * Akkauntni o'chirish tasdig'i (`DELETE /api/me`, `docs/07` §5.5) — CLAUDE.md "MAXSUS
 * DIQQAT" 1: xavfli amal har doim tasdiq dialogi bilan.
 *
 * Oqibatlar ATAYLAB to'liq yozilgan: profil anonimlashtiriladi, kabinetdagi natijalar
 * ko'rinmay qoladi, shu Telegram akkaunti bilan qayta kirilsa **YANGI** akkaunt ochiladi
 * (eski natijalar unga bog'lanmaydi). Amal qaytarilmaydi.
 */
export function DeleteAccountDialog({
  open,
  onClose,
  onConfirm,
  isDeleting,
  error,
}: DeleteAccountDialogProps) {
  const { t } = useTranslation();

  return (
    <ConfirmDialog
      open={open}
      onClose={onClose}
      onConfirm={onConfirm}
      title={t('account.delete.title')}
      description={t('account.delete.description')}
      warning={t('account.delete.warning')}
      confirmLabel={t('account.delete.confirm')}
      confirmVariant="danger"
      isConfirming={isDeleting}
      error={error}
    >
      <ul className="list-disc space-y-1.5 pl-5 text-sm text-ink-soft">
        <li>{t('account.delete.consequences.profile')}</li>
        <li>{t('account.delete.consequences.results')}</li>
        <li>{t('account.delete.consequences.newAccount')}</li>
      </ul>
    </ConfirmDialog>
  );
}
