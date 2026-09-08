import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog';
import { Textarea } from '@/shared/ui/Textarea';
import {
  PUBLIC_USER_DELETION_REASONS,
  type PublicUserDeletionReason,
} from '@/shared/config/accountDeletion';

const COMMENT_MAX_LENGTH = 500;

export interface DeleteAccountConfirmPayload {
  reason: PublicUserDeletionReason;
  comment?: string;
}

export interface DeleteAccountDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: (payload: DeleteAccountConfirmPayload) => void;
  isDeleting?: boolean;
  error?: string;
}

/**
 * Akkauntni o'chirish oqimi (`DELETE /api/me`, `docs/07` §5.5) — CLAUDE.md "MAXSUS
 * DIQQAT" 1: xavfli amal har doim tasdiq dialogi bilan. **Ikki qadamli** (egasining talabi,
 * 2026-09-08):
 *
 * 1. **Tasdiq** — oqibatlar ro'yxati (profil anonimlashtiriladi, natijalar ko'rinmay
 *    qoladi, shu Telegram akkaunti bilan qayta kirilsa YANGI akkaunt ochiladi). Tugma
 *    hali O'CHIRMAYDI — faqat sabab qadamiga o'tadi.
 * 2. **Sabab** — 5 ta radiodan biri MAJBURIY tanlanadi (`shared/config/accountDeletion.ts`
 *    — ikkita feature ishlatgani uchun `shared/`da, matn `deletionReason.<Kod>` i18n
 *    kaliti orqali admin dialogi bilan BITTA joyda). Izoh ixtiyoriy, lekin `Other`
 *    tanlanganda majburiy.
 *
 * Dialog istalgan yo'l bilan yopilsa (X, `Escape`, fon, "Bekor qilish") holat tozalanadi —
 * qayta ochilganda har doim 1-qadamdan boshlanadi.
 */
export function DeleteAccountDialog({
  open,
  onClose,
  onConfirm,
  isDeleting,
  error,
}: DeleteAccountDialogProps) {
  const { t } = useTranslation();
  const [step, setStep] = useState<1 | 2>(1);
  const [reason, setReason] = useState<PublicUserDeletionReason | null>(null);
  const [comment, setComment] = useState('');
  const [showCommentError, setShowCommentError] = useState(false);

  function handleClose() {
    // Yopilish yo'lidan qat'i nazar (X, Escape, fon, Bekor qilish) — keyingi ochilish
    // har doim 1-qadamdan boshlansin.
    setStep(1);
    setReason(null);
    setComment('');
    setShowCommentError(false);
    onClose();
  }

  function handleStep2Submit() {
    if (!reason) return;
    const trimmed = comment.trim();
    if (reason === 'Other' && trimmed.length === 0) {
      setShowCommentError(true);
      return;
    }
    onConfirm({ reason, comment: trimmed.length > 0 ? trimmed : undefined });
  }

  // Ikkala qadam BITTA `ConfirmDialog`/`Dialog` daraxtida (props step bo'yicha almashadi) —
  // aks holda alohida elementlar native `<dialog>`ni qayta o'rnatib, fokus/animatsiyani
  // uzib qo'yardi.
  return (
    <ConfirmDialog
      open={open}
      onClose={handleClose}
      onConfirm={step === 1 ? () => setStep(2) : handleStep2Submit}
      title={step === 1 ? t('account.delete.title') : t('account.delete.reasonStepTitle')}
      description={
        step === 1 ? t('account.delete.description') : t('account.delete.reasonStepLead')
      }
      warning={step === 1 ? t('account.delete.warning') : undefined}
      confirmLabel={step === 1 ? t('account.delete.continueCta') : t('account.delete.confirm')}
      confirmVariant="danger"
      isConfirming={step === 2 ? isDeleting : false}
      isConfirmDisabled={step === 2 && !reason}
      error={step === 2 ? error : undefined}
    >
      {step === 1 ? (
        <ul className="list-disc space-y-1.5 pl-5 text-sm text-ink-soft">
          <li>{t('account.delete.consequences.profile')}</li>
          <li>{t('account.delete.consequences.results')}</li>
          <li>{t('account.delete.consequences.newAccount')}</li>
        </ul>
      ) : (
        <>
          <fieldset className="flex flex-col gap-2">
            <legend className="mb-1.5 text-sm font-medium text-ink-soft">
              {t('account.delete.reasonLegend')}
            </legend>
            <div className="flex flex-col gap-1.5">
              {PUBLIC_USER_DELETION_REASONS.map((code) => (
                <label
                  key={code}
                  className="flex min-h-11 cursor-pointer items-center gap-3 rounded-2xl border border-line px-3 py-1.5 text-sm text-ink transition-colors has-[:checked]:border-firuza-400 has-[:checked]:bg-firuza-50/40"
                >
                  <input
                    type="radio"
                    name="account-delete-reason"
                    value={code}
                    checked={reason === code}
                    onChange={() => {
                      setReason(code);
                      setShowCommentError(false);
                    }}
                    className="size-5 shrink-0 border-ink-faint text-firuza-600 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-firuza-600"
                  />
                  {t(`deletionReason.${code}`)}
                </label>
              ))}
            </div>
          </fieldset>

          <Textarea
            className="mt-3"
            label={
              reason === 'Other'
                ? t('account.delete.commentRequiredLabel')
                : t('account.delete.commentLabel')
            }
            value={comment}
            maxLength={COMMENT_MAX_LENGTH}
            onChange={(event) => {
              setComment(event.target.value);
              if (showCommentError) setShowCommentError(false);
            }}
            error={showCommentError ? t('account.delete.commentRequiredError') : undefined}
          />
        </>
      )}
    </ConfirmDialog>
  );
}
