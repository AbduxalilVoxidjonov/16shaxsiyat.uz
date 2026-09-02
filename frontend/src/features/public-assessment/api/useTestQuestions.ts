import { useQuery } from '@tanstack/react-query';
import { publicRequest } from '@/shared/api/publicClient';
import { QUERY_KEYS } from '@/shared/config/queryKeys';
import type { TestQuestionsResponse } from '@/shared/api/types';

/**
 * `GET /sessions/tests/{testCode}/questions?page=` — docs/07-api-shartnoma.md, 1.5-bo'lim.
 * `enabled` — testni `start` qilib bo'lgach (`pageSize`/`totalPages` ma'lum bo'lgach) chaqiriladi.
 */
export function useTestQuestions(testCode: string, page: number, enabled: boolean) {
  return useQuery({
    queryKey: QUERY_KEYS.publicTestQuestions(testCode, page),
    queryFn: ({ signal }) =>
      publicRequest<TestQuestionsResponse>(
        `/api/public/sessions/tests/${encodeURIComponent(testCode)}/questions?page=${String(page)}`,
        { signal },
      ),
    enabled,
    retry: false,
  });
}
