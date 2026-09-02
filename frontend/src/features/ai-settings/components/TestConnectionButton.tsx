import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { CheckCircle2, XCircle } from 'lucide-react';
import { Button } from '@/shared/ui/Button';
import { AppError } from '@/shared/api/AppError';
import { useTestAiProviderConnection } from '../api/useTestAiProviderConnection';
import type { AiProviderKind, TestAiConnectionResult } from '../model/types';

export interface TestConnectionButtonProps {
  provider: AiProviderKind;
  disabled?: boolean;
}

/**
 * "Aloqani tekshirish" — `docs/11` A-7, `prompts/28` MAXSUS DIQQAT #3: natija aniq bo'lishi
 * kerak — muvaffaqiyat ("Ishlayapti · N ms") yoki xato **TURI** bilan (backend `message`
 * maydonida "kalit noto'g'ri" / "limit tugagan" / "javob bermadi" kabi aniq matn qaytaradi —
 * bu yerda umumiy "Xatolik" bilan ALMASHTIRILMAYDI, aynan ko'rsatiladi).
 */
export function TestConnectionButton({ provider, disabled }: TestConnectionButtonProps) {
  const { t } = useTranslation();
  const testConnection = useTestAiProviderConnection();
  const [result, setResult] = useState<TestAiConnectionResult | null>(null);
  const [networkError, setNetworkError] = useState<string | null>(null);

  async function handleClick() {
    setResult(null);
    setNetworkError(null);
    try {
      const response = await testConnection.mutateAsync(provider);
      setResult(response);
    } catch (caught) {
      setNetworkError(
        caught instanceof AppError ? caught.message : t('aiSettings.provider.testConnection.networkError'),
      );
    }
  }

  return (
    <div className="flex flex-col gap-2">
      <Button
        type="button"
        variant="outline"
        size="sm"
        onClick={() => void handleClick()}
        isLoading={testConnection.isPending}
        disabled={disabled}
      >
        {t('aiSettings.provider.testConnection.cta')}
      </Button>

      {result?.ok && (
        <p role="status" className="flex items-center gap-1.5 text-sm font-medium text-success-700">
          <CheckCircle2 size={16} aria-hidden="true" />
          {t('aiSettings.provider.testConnection.success', { latency: result.latencyMs ?? 0 })}
        </p>
      )}

      {result && !result.ok && (
        <p role="alert" className="flex items-center gap-1.5 text-sm font-medium text-danger-700">
          <XCircle size={16} aria-hidden="true" />
          {result.message ?? t('aiSettings.provider.testConnection.unknownError')}
        </p>
      )}

      {networkError && (
        <p role="alert" className="flex items-center gap-1.5 text-sm font-medium text-danger-700">
          <XCircle size={16} aria-hidden="true" />
          {networkError}
        </p>
      )}
    </div>
  );
}
