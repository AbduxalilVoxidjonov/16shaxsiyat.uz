import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { AiProviderConfigDto, AiProviderKind } from '../model/types';
import { AI_SETTINGS_QUERY_KEYS } from './aiSettingsKeys';

/** `POST /api/admin/ai/providers/{provider}/set-default` — docs/07, 3.5-bo'lim. */
export function useSetDefaultAiProvider() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (provider: AiProviderKind) =>
      adminRequest<AiProviderConfigDto>(`/api/admin/ai/providers/${provider}/set-default`, {
        method: 'POST',
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: AI_SETTINGS_QUERY_KEYS.providers() });
    },
  });
}
