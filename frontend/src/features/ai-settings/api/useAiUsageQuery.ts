import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { AiUsageStatsResponse } from '../model/types';
import { AI_SETTINGS_QUERY_KEYS } from './aiSettingsKeys';

const LIST_STALE_TIME_MS = 30_000;

function buildQueryString(from: string, to: string): string {
  const params = new URLSearchParams();
  if (from) params.set('from', from);
  if (to) params.set('to', to);
  return params.toString();
}

/** `GET /api/admin/ai/usage?from=&to=` — docs/07, 3.5-bo'lim: joriy oy statistikasi. */
export function useAiUsageQuery(from: string, to: string) {
  return useQuery({
    queryKey: AI_SETTINGS_QUERY_KEYS.usage(from, to),
    queryFn: ({ signal }) => {
      const qs = buildQueryString(from, to);
      return adminRequest<AiUsageStatsResponse>(`/api/admin/ai/usage${qs ? `?${qs}` : ''}`, {
        signal,
      });
    },
    staleTime: LIST_STALE_TIME_MS,
  });
}
