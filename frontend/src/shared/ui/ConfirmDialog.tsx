import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Dialog } from './Dialog';
import { Button, type ButtonVariant } from './Button';

export interface ConfirmDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title: ReactNode;
  description?: ReactNode;
  /** Qo'shimcha ogohlantirish bloki (masalan havolani yangilashda "eski havola ishlamay qoladi"). */
  warning?: ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  confirmVariant?: ButtonVariant;
  isConfirming?: boolean;
  /** Tasdiqlashdan keyingi xato matni (agar mutatsiya muvaffaqiyatsiz bo'lsa). */
  error?: string;
  /**
   * Ogohlantirish va xato orasidagi ixtiyoriy qo'shimcha blok — masalan "bu amal nechta
   * maktabni havolasiz qoldiradi" ro'yxati (`ProgramImpactNotice`, 2026-09-03). `warning`
   * `<p>` ichida render qilingani uchun ro'yxat/blok elementlarini u yerga qo'yib bo'lmaydi.
   */
  children?: ReactNode;
}

/**
 * Xavfli amallar uchun umumiy tasdiq dialogi (`Dialog` ustida) — CLAUDE.md "MAXSUS DIQQAT" 1:
 * "Xavfli amallar har doim tasdiq dialogi bilan" (havolani yangilash, maktab/savol o'chirish
 * va h.k.). `Dialog` allaqachon fokus tuzog'i, `Escape` va fokus qaytarishni ta'minlaydi
 * (native `<dialog>`), bu komponent faqat tasdiqlash/bekor qilish naqshini standartlashtiradi.
 */
export function ConfirmDialog({
  open,
  onClose,
  onConfirm,
  title,
  description,
  warning,
  confirmLabel,
  cancelLabel,
  confirmVariant = 'danger',
  isConfirming = false,
  error,
  children,
}: ConfirmDialogProps) {
  const { t } = useTranslation();

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={title}
      description={description}
      footer={
        <>
          <Button variant="outline" onClick={onClose} disabled={isConfirming}>
            {cancelLabel ?? t('common.cancel')}
          </Button>
          <Button variant={confirmVariant} onClick={onConfirm} isLoading={isConfirming}>
            {confirmLabel ?? t('common.confirm')}
          </Button>
        </>
      }
    >
      {warning && (
        <p role="alert" className="rounded-lg bg-warning-50 p-3 text-sm text-warning-700">
          {warning}
        </p>
      )}
      {children && <div className="mt-2">{children}</div>}
      {error && (
        <p role="alert" className="mt-2 text-sm text-danger-600">
          {error}
        </p>
      )}
    </Dialog>
  );
}
