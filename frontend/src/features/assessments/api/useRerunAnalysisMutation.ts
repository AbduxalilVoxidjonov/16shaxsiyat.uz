import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import { AppError } from '@/shared/api/AppError';
import type { AiProvider } from '../model/types';
import { ASSESSMENTS_QUERY_KEYS } from './assessmentsKeys';

export interface RerunAnalysisInput {
  assessmentId: string;
  /** `null` — provayder tanlanmagan: backend standart zanjirni o'zi ishlatadi (`docs/09` 8-bo'lim). */
  provider: AiProvider | null;
}

/**
 * `POST /api/admin/assessments/{id}/rerun-analysis` — `docs/07` 3.3-bo'lim. Muvaffaqiyat
 * javobi **`202 Accepted`** (tahlil fon navbatiga qo'yildi).
 *
 * `shared/api/client.ts` `202` ni umumiy holatda "hali tayyor emas" (`NOT_READY`) deb
 * `AppError` ga o'giradi — bu semantika `GET /sessions/result` uchun. Shu endpoint uchun
 * `202` — MUVAFFAQIYAT, shuning uchun aynan shu bitta holat yutiladi; qolgan xatolar
 * (`404`, `409 ASSESSMENT_INVALID_TRANSITION`, …) o'zgarishsiz qayta uloqtiriladi.
 */
async function rerunAnalysis({ assessmentId, provider }: RerunAnalysisInput): Promise<void> {
  try {
    await adminRequest<void>(`/api/admin/assessments/${assessmentId}/rerun-analysis`, {
      method: 'POST',
      body: provider ? { provider } : {},
    });
  } catch (error) {
    if (error instanceof AppError && error.status === 202) {
      return;
    }
    throw error;
  }
}

export function useRerunAnalysisMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: rerunAnalysis,
    onSuccess: (_data, { assessmentId }) => {
      void queryClient.invalidateQueries({
        queryKey: ASSESSMENTS_QUERY_KEYS.detail(assessmentId),
      });
    },
  });
}
