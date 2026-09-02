import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { AiProviderConfigDto } from '../model/types';
import { AI_SETTINGS_QUERY_KEYS } from './aiSettingsKeys';

/** Ro'yxatlar uchun `staleTime` — docs/10, 5.2-bo'lim: "ro'yxatlar 30s". */
const LIST_STALE_TIME_MS = 30_000;

/** `GET /api/admin/ai/providers` — docs/07, 3.5-bo'lim. Kalitlar maskalangan holda keladi. */
export function useAiProvidersQuery() {
  return useQuery({
    queryKey: AI_SETTINGS_QUERY_KEYS.providers(),
    queryFn: ({ signal }) =>
      adminRequest<AiProviderConfigDto[]>('/api/admin/ai/providers', { signal }),
    staleTime: LIST_STALE_TIME_MS,
  });
}
