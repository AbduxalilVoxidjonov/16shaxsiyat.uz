import { useQuery } from '@tanstack/react-query';
import { publicUserRequest } from '@/shared/api/publicUserClient';
import { QUERY_KEYS } from '@/shared/config/queryKeys';
import type { MyAssessmentsResponse } from '@/shared/api/types';

/**
 * `GET /api/me/assessments` — `docs/07` §5.2. Sahifalash yo'q (bitta foydalanuvchida
 * sessiyalar soni kichik), tartib `startedAt` bo'yicha kamayish — backend beradi.
 */
export function useMyAssessments(enabled = true) {
  return useQuery({
    queryKey: QUERY_KEYS.publicUserAssessments(),
    queryFn: ({ signal }) =>
      publicUserRequest<MyAssessmentsResponse>('/api/me/assessments', { signal }),
    enabled,
  });
}
