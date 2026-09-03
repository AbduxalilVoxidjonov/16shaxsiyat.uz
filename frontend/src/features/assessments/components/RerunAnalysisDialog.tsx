import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/ui/Button';
import { Dialog } from '@/shared/ui/Dialog';
import { Select } from '@/shared/ui/Select';
import type { AiProvider } from '../model/types';

export interface RerunAnalysisDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: (provider: AiProvider | null) => void;
  isSubmitting: boolean;
  error?: string;
}

/**
 * `docs/04` 2.8-bo'lim: `AiAnalysis.Provider` — `Gemini`, `OpenAi`, `Anthropic`.
 * Bo'sh tanlov ("avtomatik") — so'rovda `provider` umuman yuborilmaydi va backend o'z
 * fallback zanjirini ishlatadi (`docs/09` 8-bo'lim). Faqat kaliti kiritilgan providerlarni
 * ko'rsatish uchun `GET /api/admin/ai/providers` kerak — u boshqa feature hududida
 * (`features/ai-settings`), shu sabab bu yerda uchta ma'lum qiymat statik turadi.
 */
const PROVIDER_OPTIONS: AiProvider[] = ['Gemini', 'OpenAi', 'Anthropic'];

const AUTO_PROVIDER = '';

/** Xavfli/qimmat amal — `CLAUDE.md` "MAXSUS DIQQAT" 1: har doim tasdiq oynasi bilan. */
export function RerunAnalysisDialog({
  open,
  onClose,
  onConfirm,
  isSubmitting,
  error,
}: RerunAnalysisDialogProps) {
  const { t } = useTranslation();
  const [provider, setProvider] = useState<string>(AUTO_PROVIDER);

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('assessmentDetail.rerunDialog.title')}
      description={t('assessmentDetail.rerunDialog.description')}
      footer={
        <>
          <Button variant="outline" onClick={onClose} disabled={isSubmitting}>
            {t('common.cancel')}
          </Button>
          <Button
            variant="primary"
            isLoading={isSubmitting}
            onClick={() => {
              onConfirm(provider === AUTO_PROVIDER ? null : (provider as AiProvider));
            }}
          >
            {t('assessmentDetail.rerunDialog.submitCta')}
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-3">
        <p className="rounded-lg bg-warning-50 p-3 text-sm text-warning-800">
          {t('assessmentDetail.rerunDialog.warning')}
        </p>
        <Select
          label={t('assessmentDetail.rerunDialog.providerLabel')}
          hint={t('assessmentDetail.rerunDialog.providerHint')}
          value={provider}
          onChange={(event) => setProvider(event.target.value)}
          options={[
            { value: AUTO_PROVIDER, label: t('assessmentDetail.rerunDialog.providerAuto') },
            ...PROVIDER_OPTIONS.map((option) => ({ value: option, label: option })),
          ]}
        />
        {error && (
          <p role="alert" className="text-sm text-danger-600">
            {error}
          </p>
        )}
      </div>
    </Dialog>
  );
}
