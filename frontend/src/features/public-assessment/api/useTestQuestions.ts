import { useQuery } from '@tanstack/react-query';
import { publicRequest } from '@/shared/api/publicClient';
import { QUERY_KEYS } from '@/shared/config/queryKeys';
import type { BranchingTestQuestionsData } from '@/shared/api/branchingTypes';

/**
 * `GET /sessions/tests/{testCode}/questions?page=` — docs/07-api-shartnoma.md, 1.5-bo'lim,
 * `docs/18` §4.1 (bo'limli anketada `sections`/tarmoqlanish maydonlari bilan kengaytirilgan —
 * `BranchingTestQuestionsData`, hozircha `schema.d.ts`da yo'q, `branchingTypes.ts`ga qarang).
 * `enabled` — testni `start` qilib bo'lgach (`pageSize`/`totalPages` ma'lum bo'lgach) chaqiriladi.
 */
export function useTestQuestions(testCode: string, page: number, enabled: boolean) {
  return useQuery({
    queryKey: QUERY_KEYS.publicTestQuestions(testCode, page),
    queryFn: ({ signal }) =>
      publicRequest<BranchingTestQuestionsData>(
        `/api/public/sessions/tests/${encodeURIComponent(testCode)}/questions?page=${String(page)}`,
        { signal },
      ),
    enabled,
    retry: false,
  });
}
