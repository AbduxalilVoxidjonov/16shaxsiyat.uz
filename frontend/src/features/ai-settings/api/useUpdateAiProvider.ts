import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { AiProviderConfigDto, AiProviderKind, UpdateAiProviderRequest } from '../model/types';
import { AI_SETTINGS_QUERY_KEYS } from './aiSettingsKeys';

export interface UpdateAiProviderInput {
  provider: AiProviderKind;
  payload: UpdateAiProviderRequest;
}

/**
 * `PUT /api/admin/ai/providers/{provider}` — docs/07, 3.5-bo'lim. `payload.apiKey`
 * `undefined` bo'lsa (kalit maydoni bo'sh qoldirilgan) `client.ts` uni `JSON.stringify`
 * paytida so'rov tanasidan butunlay olib tashlaydi — mavjud kalit backendda o'zgarmaydi
 * (`prompts/28` MAXSUS DIQQAT #1).
 */
export function useUpdateAiProvider() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ provider, payload }: UpdateAiProviderInput) =>
      adminRequest<AiProviderConfigDto>(`/api/admin/ai/providers/${provider}`, {
        method: 'PUT',
        body: payload,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: AI_SETTINGS_QUERY_KEYS.providers() });
    },
  });
}
