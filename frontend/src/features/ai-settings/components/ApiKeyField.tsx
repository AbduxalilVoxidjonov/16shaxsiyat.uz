import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Input } from '@/shared/ui/Input';
import { Button } from '@/shared/ui/Button';

export interface ApiKeyFieldProps {
  /** Backendning maskalangan qiymati (`AIza••••7f2b`) yoki kalit umuman kiritilmagan bo'lsa `null`. */
  maskedValue: string | null;
  /** Joriy INPUT qiymati (react-hook-form orqali boshqariladi) — bo'sh bo'lsa kalit o'zgarmaydi. */
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  error?: string;
}

/**
 * API kalit maydoni — **eng nozik ekran** (`prompts/28` MAXSUS DIQQAT #1-2, `CLAUDE.md`).
 *
 * Ikki holat:
 * - **Maskalangan ko'rinish** (kalit oldin kiritilgan): to'liq qiymat HECH QACHON ko'rsatilmaydi
 *   (faqat backend maskasi, masalan `AIza••••7f2b`), nusxalash tugmasi YO'Q. "O'zgartirish"
 *   bosilsa bo'sh `password` maydon ochiladi.
 * - **Tahrirlash holati** (kalit yo'q yoki "O'zgartirish" bosilgan): `type="password"`,
 *   `autoComplete="off"`, ostida **aniq** ogohlantirish — bo'sh qoldirilsa mavjud kalit
 *   o'zgarmaydi (aks holda admin uni tasodifan o'chirib qo'yishi va AI jimgina ishlamay
 *   qolishi mumkin).
 */
export function ApiKeyField({ maskedValue, value, onChange, disabled, error }: ApiKeyFieldProps) {
  const { t } = useTranslation();
  const [isEditing, setIsEditing] = useState(maskedValue === null);

  if (!isEditing) {
    return (
      <div className="flex flex-col gap-1.5">
        <span className="text-sm font-medium text-neutral-700">
          {t('aiSettings.provider.apiKeyLabel')}
        </span>
        <div className="flex items-center gap-2">
          <span
            data-testid="masked-api-key"
            className="flex-1 truncate rounded-lg border border-neutral-300 bg-neutral-50 px-3 py-2.5 font-mono text-sm text-neutral-700"
          >
            {maskedValue}
          </span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={disabled}
            onClick={() => {
              onChange('');
              setIsEditing(true);
            }}
          >
            {t('aiSettings.provider.apiKeyChangeCta')}
          </Button>
        </div>
        <p className="text-xs text-neutral-500">{t('aiSettings.provider.apiKeyMaskedHint')}</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-1.5">
      <Input
        type="password"
        autoComplete="off"
        label={t('aiSettings.provider.apiKeyLabel')}
        placeholder={t('aiSettings.provider.apiKeyPlaceholder')}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        disabled={disabled}
        error={error}
        hint={t('aiSettings.provider.apiKeyBlankHint')}
      />
      {maskedValue !== null && (
        <button
          type="button"
          className="self-start text-xs font-medium text-primary-600 hover:underline"
          onClick={() => {
            onChange('');
            setIsEditing(false);
          }}
        >
          {t('common.cancel')}
        </button>
      )}
    </div>
  );
}
