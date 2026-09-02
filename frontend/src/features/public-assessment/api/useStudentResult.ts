import { useQuery } from '@tanstack/react-query';
import { publicRequest } from '@/shared/api/publicClient';
import { QUERY_KEYS } from '@/shared/config/queryKeys';
import type { StudentResultResponse } from '@/shared/api/types';

/**
 * `GET /sessions/result` — docs/07-api-shartnoma.md, 1.9-bo'lim. **P12 hali yozilmoqda**
 * (`shared/api/types.ts`dagi `StudentResultResponse` izohiga qarang).
 * `202` (hali tayyor emas) va `403` (ko'rsatish o'chirilgan) — domen javoblari, qayta urinish
 * foydasiz, shu sabab `retry: false`; `StudentResultPage` ularni `error.status` orqali ajratadi.
 */
export function useStudentResult(enabled = true) {
  return useQuery({
    queryKey: QUERY_KEYS.publicStudentResult(),
    queryFn: ({ signal }) =>
      publicRequest<StudentResultResponse>('/api/public/sessions/result', { signal }),
    enabled,
    retry: false,
  });
}
