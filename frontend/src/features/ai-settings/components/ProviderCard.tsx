import { useEffect, useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { Star } from 'lucide-react';
import { Card } from '@/shared/ui/Card';
import { Input } from '@/shared/ui/Input';
import { Checkbox } from '@/shared/ui/Checkbox';
import { Button } from '@/shared/ui/Button';
import { ConfirmDialog } from '@/shared/ui/ConfirmDialog';
import { useToast } from '@/shared/ui/useToast';
import { AppError } from '@/shared/api/AppError';
import { useUpdateAiProvider } from '../api/useUpdateAiProvider';
import { useSetDefaultAiProvider } from '../api/useSetDefaultAiProvider';
import { providerFormSchema, type ProviderFormValues } from '../model/providerFormSchema';
import { RECOMMENDED_AI_MODELS } from '../model/providerMeta';
import { getProviderStatus } from '../model/providerStatus';
import type { AiProviderConfigDto, AiProviderKind } from '../model/types';
import { ApiKeyField } from './ApiKeyField';
import { ProviderStatusBadge } from './ProviderStatusBadge';
import { TestConnectionButton } from './TestConnectionButton';

export interface ProviderCardProps {
  provider: AiProviderKind;
  /** `null` — bu provayder uchun hali yozuv yaratilmagan (kalit ham, sozlama ham yo'q). */
  config: AiProviderConfigDto | null;
}

const DEFAULT_MAX_OUTPUT_TOKENS = 4096;
const DEFAULT_TEMPERATURE = 0.4;

function toFormValues(config: AiProviderConfigDto | null, provider: AiProviderKind): ProviderFormValues {
  return {
    model: config?.model ?? RECOMMENDED_AI_MODELS[provider],
    maxOutputTokens: config?.maxOutputTokens ?? DEFAULT_MAX_OUTPUT_TOKENS,
    temperature: config?.temperature ?? DEFAULT_TEMPERATURE,
    isActive: config?.isActive ?? false,
    apiKey: '',
  };
}

/**
 * Bitta provayder kartasi — `docs/11` A-7. O'z ichida mustaqil forma (RHF + zod):
 * model/tokens/temperature/faollik, API kalit maydoni (`ApiKeyField`), "Aloqani tekshirish"
 * va "Default qilish". Kalit almashtirish va default o'zgartirish — **xavfli amallar**,
 * tasdiq dialogi bilan (`CLAUDE.md` QAT'IY QOIDALAR).
 */
export function ProviderCard({ provider, config }: ProviderCardProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const updateProvider = useUpdateAiProvider();
  const setDefault = useSetDefaultAiProvider();

  const {
    register,
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ProviderFormValues>({
    resolver: zodResolver(providerFormSchema),
    defaultValues: toFormValues(config, provider),
  });

  useEffect(() => {
    reset(toFormValues(config, provider));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [config, provider]);

  const [pendingKeyChange, setPendingKeyChange] = useState<ProviderFormValues | null>(null);
  const [confirmingDefault, setConfirmingDefault] = useState(false);

  async function submitValues(values: ProviderFormValues) {
    try {
      await updateProvider.mutateAsync({
        provider,
        payload: {
          apiKey: values.apiKey ? values.apiKey : undefined,
          model: values.model,
          maxOutputTokens: values.maxOutputTokens,
          temperature: values.temperature,
          isActive: values.isActive,
          fallbackOrder: config?.fallbackOrder ?? 100,
        },
      });
      toast.show({ variant: 'success', title: t('aiSettings.provider.saveSuccess') });
      setPendingKeyChange(null);
    } catch (caught) {
      toast.show({
        variant: 'danger',
        title: caught instanceof AppError ? caught.message : t('aiSettings.provider.saveError'),
      });
    }
  }

  const onSubmit = handleSubmit((values) => {
    if (values.apiKey) {
      // Kalit almashtirish — xavfli amal, tasdiqlash talab qilinadi.
      setPendingKeyChange(values);
      return;
    }
    void submitValues(values);
  });

  async function handleConfirmKeyChange() {
    if (!pendingKeyChange) return;
    await submitValues(pendingKeyChange);
  }

  async function handleConfirmSetDefault() {
    try {
      await setDefault.mutateAsync(provider);
      toast.show({ variant: 'success', title: t('aiSettings.provider.setDefaultSuccess') });
    } catch (caught) {
      toast.show({
        variant: 'danger',
        title: caught instanceof AppError ? caught.message : t('aiSettings.provider.setDefaultError'),
      });
    } finally {
      setConfirmingDefault(false);
    }
  }

  const status = getProviderStatus(config);
  const canSetDefault = status !== 'no-key' && !config?.isDefault;

  return (
    <Card
      title={
        <span className="flex items-center gap-2">
          {t(`aiSettings.provider.names.${provider}`)}
          <ProviderStatusBadge status={status} />
        </span>
      }
      actions={
        <Button
          type="button"
          variant="ghost"
          size="sm"
          disabled={!canSetDefault || setDefault.isPending}
          onClick={() => setConfirmingDefault(true)}
        >
          <Star size={14} aria-hidden="true" />
          {t('aiSettings.provider.setDefaultCta')}
        </Button>
      }
    >
      <form onSubmit={(event) => void onSubmit(event)} noValidate className="flex flex-col gap-4">
        <Input
          label={t('aiSettings.provider.modelLabel')}
          hint={t('aiSettings.provider.modelHint', { recommended: RECOMMENDED_AI_MODELS[provider] })}
          error={errors.model?.message}
          {...register('model')}
        />
        <div className="grid grid-cols-2 gap-3">
          <Input
            type="number"
            label={t('aiSettings.provider.maxOutputTokensLabel')}
            error={errors.maxOutputTokens?.message}
            {...register('maxOutputTokens', { valueAsNumber: true })}
          />
          <Input
            type="number"
            step="0.1"
            label={t('aiSettings.provider.temperatureLabel')}
            error={errors.temperature?.message}
            {...register('temperature', { valueAsNumber: true })}
          />
        </div>

        <Controller
          control={control}
          name="apiKey"
          render={({ field }) => (
            <ApiKeyField
              key={config?.maskedApiKey ?? 'no-key'}
              maskedValue={config?.maskedApiKey ?? null}
              value={field.value}
              onChange={field.onChange}
              error={errors.apiKey?.message}
            />
          )}
        />

        <Checkbox label={t('aiSettings.provider.isActiveLabel')} {...register('isActive')} />

        <div className="flex items-center justify-between gap-3">
          <TestConnectionButton provider={provider} />
          <Button type="submit" isLoading={updateProvider.isPending}>
            {t('common.save')}
          </Button>
        </div>
      </form>

      {pendingKeyChange && (
        <ConfirmDialog
          open
          onClose={() => setPendingKeyChange(null)}
          onConfirm={() => void handleConfirmKeyChange()}
          title={t('aiSettings.provider.confirmKeyChangeTitle')}
          description={t('aiSettings.provider.confirmKeyChangeDescription')}
          confirmLabel={t('aiSettings.provider.confirmKeyChangeCta')}
          confirmVariant="danger"
          isConfirming={updateProvider.isPending}
        />
      )}

      {confirmingDefault && (
        <ConfirmDialog
          open
          onClose={() => setConfirmingDefault(false)}
          onConfirm={() => void handleConfirmSetDefault()}
          title={t('aiSettings.provider.confirmDefaultTitle', {
            name: t(`aiSettings.provider.names.${provider}`),
          })}
          description={t('aiSettings.provider.confirmDefaultDescription')}
          confirmLabel={t('aiSettings.provider.confirmDefaultCta')}
          confirmVariant="primary"
          isConfirming={setDefault.isPending}
        />
      )}
    </Card>
  );
}
