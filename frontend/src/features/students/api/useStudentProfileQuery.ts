import { useState } from 'react';
import { useQuery, type Query, type UseQueryResult } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import { useAnalysisPolling } from '@/shared/hooks/useAnalysisPolling';
import type { StudentProfileResponse } from '../model/profileTypes';
import { STUDENTS_QUERY_KEYS } from './studentsKeys';

/** Joriy (`isLatest: true`) sessiyaning holati — polling shu asosda hal qilinadi. */
function latestAssessmentStatus(data: StudentProfileResponse | undefined): string | undefined {
  return data?.assessments.find((assessment) => assessment.isLatest)?.status;
}

export interface StudentProfileQueryResult {
  query: UseQueryResult<StudentProfileResponse>;
  /** Joriy sessiya `Analyzing` holatidami. */
  isAnalyzing: boolean;
  /** Kutish cheklovi tugadi — so'rash to'xtadi, foydalanuvchiga xabar ko'rsatiladi. */
  pollTimedOut: boolean;
}

/**
 * `GET /api/admin/students/{id}` — docs/07, 3.2-bo'lim, individual profil. `staleTime: 0`
 * (docs/10, 5.2-bo'lim: "profil — har doim yangi").
 *
 * `refetchInterval` faqat joriy sessiya `Analyzing` holatida ishlaydi (P25, 5-band) —
 * TanStack Query `refetchInterval` funksiya shaklida so'raladi, shu sabab har javobdan
 * keyin o'zi qayta baholanadi va status `Analyzed`/`AnalysisFailed` ga o'tganda avtomatik
 * to'xtaydi.
 *
 * Bunga qo'shimcha ikki cheklov `useAnalysisPolling` dan keladi: sahifa fonga o'tsa
 * (`document.hidden`) so'ralmaydi va 3 daqiqadan keyin butunlay to'xtaydi (`pollTimedOut`) —
 * cheksiz polling batareyani va serverni behuda yeydi.
 */
export function useStudentProfileQuery(studentId: string | undefined): StudentProfileQueryResult {
  // `isAnalyzing` so'rov javobidan kelib chiqadi, `refetchInterval` esa so'rovdan OLDIN
  // kerak — shu sabab holat alohida saqlanadi va javobdan keyin effekt bilan yangilanadi.
  const [analyzing, setAnalyzing] = useState(false);
  const polling = useAnalysisPolling(analyzing);

  const query = useQuery({
    queryKey: STUDENTS_QUERY_KEYS.profile(studentId ?? ''),
    queryFn: ({ signal }) =>
      adminRequest<StudentProfileResponse>(`/api/admin/students/${studentId ?? ''}`, { signal }),
    enabled: Boolean(studentId),
    staleTime: 0,
    refetchInterval: (queryState: Query<StudentProfileResponse>) =>
      latestAssessmentStatus(queryState.state.data) === 'Analyzing'
        ? polling.refetchInterval
        : false,
  });

  const isAnalyzing = latestAssessmentStatus(query.data) === 'Analyzing';

  // Render paytida moslash (effektda emas): javob kelgach `refetchInterval` darhol
  // qayta baholanadi, effektdagi `setState` esa ortiqcha kaskadli render beradi.
  if (analyzing !== isAnalyzing) {
    setAnalyzing(isAnalyzing);
  }

  return { query, isAnalyzing, pollTimedOut: polling.timedOut };
}
