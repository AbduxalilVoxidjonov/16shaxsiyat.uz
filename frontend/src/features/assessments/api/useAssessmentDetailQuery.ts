import { useQuery, type Query } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { AssessmentDetailDto } from '../model/types';
import { ASSESSMENTS_QUERY_KEYS } from './assessmentsKeys';

const ANALYZING_REFETCH_INTERVAL_MS = 5000;

/**
 * AI tahlili navbatda/ishlayotgan bo'lsa sahifa o'zini yangilab turadi va holat
 * `Succeeded`/`Failed` ga o'tishi bilan **o'zi to'xtaydi** (`useStudentProfileQuery`
 * bilan bir xil naqsh — cheksiz polling yo'q).
 */
function isAnalysisInFlight(data: AssessmentDetailDto | undefined): boolean {
  const status = data?.aiAnalysis?.status;
  return status === 'Pending' || status === 'Running';
}

/**
 * `GET /api/admin/assessments/{id}` — `docs/07` 3.3-bo'lim. `staleTime: 0`: sessiya holati
 * va AI tahlili fon ishi bilan o'zgaradi, shu sabab har kirishda yangi ma'lumot so'raladi.
 */
export function useAssessmentDetailQuery(assessmentId: string | undefined) {
  return useQuery({
    queryKey: ASSESSMENTS_QUERY_KEYS.detail(assessmentId ?? ''),
    queryFn: ({ signal }) =>
      adminRequest<AssessmentDetailDto>(`/api/admin/assessments/${assessmentId ?? ''}`, { signal }),
    enabled: Boolean(assessmentId),
    staleTime: 0,
    refetchInterval: (query: Query<AssessmentDetailDto>) =>
      isAnalysisInFlight(query.state.data) ? ANALYZING_REFETCH_INTERVAL_MS : false,
  });
}
