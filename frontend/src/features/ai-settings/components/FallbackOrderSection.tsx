import { useTranslation } from 'react-i18next';
import { ArrowDown, ArrowUp } from 'lucide-react';
import { Card } from '@/shared/ui/Card';
import { Button } from '@/shared/ui/Button';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { useUpdateAiProvider } from '../api/useUpdateAiProvider';
import type { AiProviderConfigDto, UpdateAiProviderRequest } from '../model/types';

export interface FallbackOrderSectionProps {
  providers: AiProviderConfigDto[];
}

function toUpdatePayload(config: AiProviderConfigDto, fallbackOrder: number): UpdateAiProviderRequest {
  return {
    // `apiKey` ATAYLAB yo'q — tartib almashtirish kalitga TEGMAYDI (`UpdateAiProviderRequest`
    // izohi: maydon yo'qligi = mavjud kalit o'zgarmaydi).
    model: config.model,
    maxOutputTokens: config.maxOutputTokens,
    temperature: config.temperature,
    isActive: config.isActive,
    fallbackOrder,
    // `baseUrl` — `apiKey`dan farqli, yuborilmasa backend uni O'CHIRADI (`UpdateSettings`).
    baseUrl: config.baseUrl,
  };
}

/**
 * Fallback tartibi — `docs/11` A-7 / `prompts/28`: "drag-and-drop ro'yxat (yoki
 * yuqoriga/pastga tugmalari)". Loyihada drag-and-drop kutubxonasi ulanmagan
 * (`ProgramTestsList.tsx`dagi xuddi shu qarorga qarang) — shu sabab yuqoriga/pastga
 * tugmalari bilan, klaviatura bilan to'liq ishlaydigan tartiblash qo'llanildi. Har harakat
 * ikkita qo'shni provayderning `fallbackOrder` qiymatini almashtiradi (backendda alohida
 * "reorder" endpoint yo'q — `docs/07` 3.5-bo'lim, har provayder o'z `PUT`i orqali yangilanadi).
 */
export function FallbackOrderSection({ providers }: FallbackOrderSectionProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const updateProvider = useUpdateAiProvider();

  const sorted = [...providers].sort((a, b) => a.fallbackOrder - b.fallbackOrder);

  async function move(index: number, direction: -1 | 1) {
    const targetIndex = index + direction;
    if (targetIndex < 0 || targetIndex >= sorted.length) return;
    const current = sorted[index];
    const target = sorted[targetIndex];
    if (!current || !target) return;

    try {
      await Promise.all([
        updateProvider.mutateAsync({
          provider: current.provider,
          payload: toUpdatePayload(current, target.fallbackOrder),
        }),
        updateProvider.mutateAsync({
          provider: target.provider,
          payload: toUpdatePayload(target, current.fallbackOrder),
        }),
      ]);
      toast.show({ variant: 'success', title: t('aiSettings.fallback.reorderSuccess') });
    } catch (caught) {
      toast.show({
        variant: 'danger',
        title: caught instanceof AppError ? caught.message : t('aiSettings.fallback.reorderError'),
      });
    }
  }

  return (
    <Card title={t('aiSettings.fallback.heading')}>
      <p className="mb-3 text-sm text-neutral-500">{t('aiSettings.fallback.description')}</p>
      <ol className="flex flex-col gap-2">
        {sorted.map((config, index) => (
          <li
            key={config.provider}
            className="flex items-center justify-between gap-3 rounded-lg border border-neutral-200 p-3"
          >
            <span className="text-sm font-medium text-neutral-900">
              {index + 1}. {t(`aiSettings.provider.names.${config.provider}`)}
            </span>
            <div className="flex items-center gap-1">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                aria-label={t('aiSettings.fallback.moveUp')}
                disabled={index === 0 || updateProvider.isPending}
                onClick={() => void move(index, -1)}
              >
                <ArrowUp size={16} aria-hidden="true" />
              </Button>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                aria-label={t('aiSettings.fallback.moveDown')}
                disabled={index === sorted.length - 1 || updateProvider.isPending}
                onClick={() => void move(index, 1)}
              >
                <ArrowDown size={16} aria-hidden="true" />
              </Button>
            </div>
          </li>
        ))}
      </ol>
    </Card>
  );
}
