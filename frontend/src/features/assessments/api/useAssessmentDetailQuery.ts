import { useState } from 'react';
import { useQuery, type Query, type UseQueryResult } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import { useAnalysisPolling } from '@/shared/hooks/useAnalysisPolling';
import type { AssessmentDetailDto } from '../model/types';
import { ASSESSMENTS_QUERY_KEYS } from './assessmentsKeys';

/**
 * AI tahlili navbatda/ishlayotganini ikki manbadan aniqlaydi: sessiya holati `Analyzing`
 * yoki AI yozuvi `Pending`/`Running`. Holat `Analyzed`/`AnalysisFailed` ga o'tishi bilan
 * polling **o'zi to'xtaydi** (`useStudentProfileQuery` bilan bir xil naqsh).
 */
function isAnalysisInFlight(data: AssessmentDetailDto | undefined): boolean {
  if (data?.status === 'Analyzing') return true;
  const status = data?.aiAnalysis?.status;
  return status === 'Pending' || status === 'Running';
}

export interface AssessmentDetailQueryResult {
  query: UseQueryResult<AssessmentDetailDto>;
  isAnalyzing: boolean;
  /** Kutish cheklovi tugadi — so'rash to'xtadi, foydalanuvchiga xabar ko'rsatiladi. */
  pollTimedOut: boolean;
}

/**
 * `GET /api/admin/assessments/{id}` — `docs/07` 3.3-bo'lim. `staleTime: 0`: sessiya holati
 * va AI tahlili fon ishi bilan o'zgaradi, shu sabab har kirishda yangi ma'lumot so'raladi.
 *
 * Polling cheklovlari `useAnalysisPolling` da: 4 s interval, 3 daqiqa chegara va sahifa
 * fonga o'tganda to'xtash — cheksiz polling batareyani va serverni behuda yeydi.
 */
export function useAssessmentDetailQuery(
  assessmentId: string | undefined,
): AssessmentDetailQueryResult {
  // `isAnalyzing` javobdan kelib chiqadi, `refetchInterval` esa so'rovdan OLDIN kerak —
  // shu sabab holat alohida saqlanadi va javobdan keyin effekt bilan yangilanadi.
  const [analyzing, setAnalyzing] = useState(false);
  const polling = useAnalysisPolling(analyzing);

  const query = useQuery({
    queryKey: ASSESSMENTS_QUERY_KEYS.detail(assessmentId ?? ''),
    queryFn: ({ signal }) =>
      adminRequest<AssessmentDetailDto>(`/api/admin/assessments/${assessmentId ?? ''}`, { signal }),
    enabled: Boolean(assessmentId),
    staleTime: 0,
    refetchInterval: (queryState: Query<AssessmentDetailDto>) =>
      isAnalysisInFlight(queryState.state.data) ? polling.refetchInterval : false,
  });

  const isAnalyzing = isAnalysisInFlight(query.data);

  // Render paytida moslash (effektda emas): javob kelgach `refetchInterval` darhol
  // qayta baholanadi, effektdagi `setState` esa ortiqcha kaskadli render beradi.
  if (analyzing !== isAnalyzing) {
    setAnalyzing(isAnalyzing);
  }

  return { query, isAnalyzing, pollTimedOut: polling.timedOut };
}
