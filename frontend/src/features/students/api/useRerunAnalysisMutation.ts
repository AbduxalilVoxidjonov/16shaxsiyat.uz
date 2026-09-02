import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import { AppError } from '@/shared/api/AppError';
import type { AiProvider } from '../model/profileTypes';
import { STUDENTS_QUERY_KEYS } from './studentsKeys';

export interface RerunAnalysisInput {
  studentId: string;
  assessmentId: string;
  provider: AiProvider;
}

/**
 * `POST /api/admin/assessments/{id}/rerun-analysis` — docs/07, 3.3-bo'lim, muvaffaqiyatli
 * so'rov `202` qaytaradi. `shared/api/client.ts` `202`ni umumiy holatda "hali tayyor emas"
 * (`NOT_READY`) deb talqin qiladi — bu semantika `GET /sessions/result` (docs/07, 1.9) uchun
 * mo'ljallangan, lekin shu endpoint uchun **aynan `202` — muvaffaqiyat natijasi** (navbatga
 * qo'yildi). Shu sabab pastda shu bitta holat (`status === 202`) muvaffaqiyat sifatida
 * yutiladi; boshqa har qanday xato o'zgarishsiz qayta uloqtiriladi.
 */
async function rerunAnalysis({ assessmentId, provider }: RerunAnalysisInput): Promise<void> {
  try {
    await adminRequest<void>(`/api/admin/assessments/${assessmentId}/rerun-analysis`, {
      method: 'POST',
      body: { provider },
    });
  } catch (error) {
    if (error instanceof AppError && error.status === 202) {
      return;
    }
    throw error;
  }
}

/**
 * Qayta tahlil so'rovi — P25, 8-band: "tasdiq → `202` → holat `Analyzing`". `onSuccess`da
 * profil so'rovi invalidatsiya qilinadi — `useStudentProfileQuery`ning `refetchInterval`i
 * yangi `Analyzing` holatini o'zi kuzatib boradi, bu yerda qo'shimcha polling kerak emas.
 */
export function useRerunAnalysisMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: rerunAnalysis,
    onSuccess: (_data, { studentId }) => {
      void queryClient.invalidateQueries({ queryKey: STUDENTS_QUERY_KEYS.profile(studentId) });
    },
  });
}
