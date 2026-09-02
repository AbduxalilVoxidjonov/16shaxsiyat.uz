import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { AiPromptTemplateDto } from '../model/types';
import { AI_SETTINGS_QUERY_KEYS } from './aiSettingsKeys';

const LIST_STALE_TIME_MS = 30_000;

/** `GET /api/admin/ai/prompts` — docs/07, 3.5-bo'lim: prompt shablon versiyalari. */
export function useAiPromptsQuery() {
  return useQuery({
    queryKey: AI_SETTINGS_QUERY_KEYS.prompts(),
    queryFn: ({ signal }) => adminRequest<AiPromptTemplateDto[]>('/api/admin/ai/prompts', { signal }),
    staleTime: LIST_STALE_TIME_MS,
  });
}
