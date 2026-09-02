import { useQuery, type Query } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { StudentProfileResponse } from '../model/profileTypes';
import { STUDENTS_QUERY_KEYS } from './studentsKeys';

/** Joriy (`isLatest: true`) sessiyaning holati — `refetchInterval` shu asosda hal qilinadi. */
function latestAssessmentStatus(data: StudentProfileResponse | undefined): string | undefined {
  return data?.assessments.find((assessment) => assessment.isLatest)?.status;
}

const ANALYZING_REFETCH_INTERVAL_MS = 5000;

/**
 * `GET /api/admin/students/{id}` — docs/07, 3.2-bo'lim, individual profil. `staleTime: 0`
 * (docs/10, 5.2-bo'lim: "profil — har doim yangi"). `refetchInterval` faqat joriy sessiya
 * `Analyzing` holatida ishlaydi (P25, 5-band va cheklovlar: "keyin o'chadi") — TanStack Query
 * `refetchInterval` funksiya shaklida so'raladi, shu sabab har javobdan keyin o'zi qayta
 * baholanadi va status `Analyzed`/`AnalysisFailed`ga o'tganda avtomatik to'xtaydi.
 */
export function useStudentProfileQuery(studentId: string | undefined) {
  return useQuery({
    queryKey: STUDENTS_QUERY_KEYS.profile(studentId ?? ''),
    queryFn: ({ signal }) =>
      adminRequest<StudentProfileResponse>(`/api/admin/students/${studentId ?? ''}`, { signal }),
    enabled: Boolean(studentId),
    staleTime: 0,
    refetchInterval: (query: Query<StudentProfileResponse>) =>
      latestAssessmentStatus(query.state.data) === 'Analyzing' ? ANALYZING_REFETCH_INTERVAL_MS : false,
  });
}
