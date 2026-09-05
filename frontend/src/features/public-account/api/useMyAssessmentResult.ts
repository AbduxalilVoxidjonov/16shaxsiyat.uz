import { useQuery } from '@tanstack/react-query';
import { publicUserRequest } from '@/shared/api/publicUserClient';
import { QUERY_KEYS } from '@/shared/config/queryKeys';
import type { StudentResultResponse } from '@/shared/api/types';

/**
 * `GET /api/me/assessments/{id}/result` — `docs/07` §5.3. Javob shakli §1.9 bilan aynan
 * bir xil, shu sabab tip ham o'sha (`StudentResultResponse`).
 *
 * `202` (hali tayyor emas), `403` (natija yopiq) va `404` (yo'q yoki begona) — domen
 * javoblari, qayta urinish foydasiz: shu sabab `retry: false`, sahifa ularni
 * `error.status` bo'yicha ajratadi.
 */
export function useMyAssessmentResult(assessmentId: string, enabled = true) {
  return useQuery({
    queryKey: QUERY_KEYS.publicUserAssessmentResult(assessmentId),
    queryFn: ({ signal }) =>
      publicUserRequest<StudentResultResponse>(
        `/api/me/assessments/${encodeURIComponent(assessmentId)}/result`,
        { signal },
      ),
    enabled: enabled && assessmentId.length > 0,
    retry: false,
  });
}
