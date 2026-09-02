import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { Skeleton } from '@/shared/ui/Skeleton';
import { ErrorState } from '@/shared/ui/ErrorState';
import { useAiProvidersQuery } from '../api/useAiProvidersQuery';
import { AI_PROVIDER_DISPLAY_ORDER } from '../model/providerMeta';
import type { AiProviderConfigDto, AiProviderKind } from '../model/types';
import { ProviderCard } from '../components/ProviderCard';
import { FallbackOrderSection } from '../components/FallbackOrderSection';
import { NoActiveProviderBanner } from '../components/NoActiveProviderBanner';
import { UsageStatsSection } from '../components/UsageStatsSection';
import { PromptVersionsSection } from '../components/PromptVersionsSection';

function mergeByProvider(
  providers: AiProviderConfigDto[] | undefined,
): Record<AiProviderKind, AiProviderConfigDto | null> {
  const byKind: Record<AiProviderKind, AiProviderConfigDto | null> = {
    Gemini: null,
    OpenAi: null,
    Anthropic: null,
  };
  for (const config of providers ?? []) {
    byKind[config.provider] = config;
  }
  return byKind;
}

/**
 * `/admin/ai` — AI sozlamalari (`docs/11` A-7, `prompts/28`). Uch provider kartasi (mustaqil
 * forma), fallback tartibi, foydalanish statistikasi, promptlar bo'limi. Hech bir provider
 * faol bo'lmasa yuqorida ogohlantirish banner (Cheklovlar bo'limi).
 */
export default function AiProvidersPage() {
  const { t } = useTranslation();
  usePageTitle(t('pages.ai.title'));

  const providersQuery = useAiProvidersQuery();
  const byKind = mergeByProvider(providersQuery.data);
  const hasActiveProvider = (providersQuery.data ?? []).some((config) => config.isActive);

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold text-neutral-900">{t('pages.ai.title')}</h1>

      {providersQuery.isSuccess && !hasActiveProvider && <NoActiveProviderBanner />}

      {providersQuery.isPending && (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
          {Array.from({ length: 3 }, (_, index) => (
            <Skeleton key={index} className="h-96 w-full" />
          ))}
        </div>
      )}

      {providersQuery.isError && <ErrorState onRetry={() => void providersQuery.refetch()} />}

      {providersQuery.isSuccess && (
        <>
          <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
            {AI_PROVIDER_DISPLAY_ORDER.map((provider) => (
              <ProviderCard key={provider} provider={provider} config={byKind[provider]} />
            ))}
          </div>

          <FallbackOrderSection
            providers={AI_PROVIDER_DISPLAY_ORDER.map(
              (provider) =>
                byKind[provider] ?? {
                  provider,
                  displayName: provider,
                  maskedApiKey: null,
                  model: '',
                  maxOutputTokens: 4096,
                  temperature: 0.4,
                  isDefault: false,
                  isActive: false,
                  fallbackOrder: 100,
                },
            )}
          />
        </>
      )}

      <UsageStatsSection />
      <PromptVersionsSection />
    </div>
  );
}
