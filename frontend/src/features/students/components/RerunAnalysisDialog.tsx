import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Dialog } from '@/shared/ui/Dialog';
import { Select } from '@/shared/ui/Select';
import { Button } from '@/shared/ui/Button';
import type { AiProvider } from '../model/profileTypes';

export interface RerunAnalysisDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: (provider: AiProvider | null) => void;
  isSubmitting: boolean;
  error?: string;
}

/**
 * `docs/04-domain-model.md`, 2.8-bo'lim: `AiAnalysis.Provider` — `Gemini`, `OpenAi`, `Anthropic`.
 *
 * P25 (7-band) "provider tanlash (faqat mavjudlari)" deydi — ya'ni superadmin kaliti
 * kiritilgan va faol qilingan providerlar. Bu ro'yxat `GET /api/admin/ai/providers`
 * (docs/07, 3.5-bo'lim) orqali olinishi kerak, lekin `/admin/ai` sahifasi (A-7) hali
 * skelet (`AiProvidersPage` — `PlaceholderPage`, P16–P18/P22 boshqa promptda) — shu sabab
 * bu yerda 3 ta ma'lum provider statik ro'yxat sifatida ko'rsatiladi. PM/backend
 * tasdiqlagach `useAvailableAiProvidersQuery` bilan almashtiriladi (hisobotda savol
 * sifatida qoldirilgan).
 */
const PROVIDER_OPTIONS: AiProvider[] = ['Gemini', 'OpenAi', 'Anthropic'];

/** Bo'sh tanlov — so'rovda `provider` yuborilmaydi, backend o'z zanjirini ishlatadi. */
const AUTO_PROVIDER = '';

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
      title={t('studentProfile.rerunDialog.title')}
      description={t('studentProfile.rerunDialog.description')}
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
            {t('studentProfile.rerunDialog.submitCta')}
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-3">
        {/* Eng ko'p so'raladigan savol: "eskisi o'chib ketadimi?" — javob dialogda turadi. */}
        <p className="rounded-lg bg-primary-50 p-3 text-sm text-primary-800">
          {t('studentProfile.rerunDialog.keepsHistoryNotice')}
        </p>
        <Select
          label={t('studentProfile.rerunDialog.providerLabel')}
          value={provider}
          onChange={(event) => setProvider(event.target.value)}
          options={[
            { value: AUTO_PROVIDER, label: t('studentProfile.rerunDialog.providerAuto') },
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
