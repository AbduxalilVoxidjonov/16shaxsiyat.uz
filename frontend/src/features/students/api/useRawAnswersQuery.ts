import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { AssessmentAnswersDto } from '../model/profileTypes';
import { STUDENTS_QUERY_KEYS } from './studentsKeys';

/**
 * `GET /api/admin/assessments/{id}/answers` — docs/07, 3.3-bo'lim, "Savolma-savol javoblar
 * va tahlili". Faqat bo'lim OCHILGANDA so'raladi (`enabled`) — ~190 qatorlik audit
 * ma'lumoti profil yuklanishida darhol kerak emas.
 *
 * `testCode` filtri BERILMAYDI: butun sessiya bir marta olinadi va bloklar mijozda
 * guruhlanadi. Sabab — backend `session`/`scales` signallarini baribir butun sessiya
 * bo'yicha hisoblaydi (`ReliabilityCalculator` sessiya darajasida ishlaydi), shu sabab
 * blok bo'yicha alohida so'rov faqat takroriy trafik berardi.
 */
export function useRawAnswersQuery(assessmentId: string | undefined, enabled: boolean) {
  return useQuery({
    queryKey: STUDENTS_QUERY_KEYS.rawAnswers(assessmentId ?? ''),
    queryFn: ({ signal }) =>
      adminRequest<AssessmentAnswersDto>(
        `/api/admin/assessments/${assessmentId ?? ''}/answers`,
        { signal },
      ),
    enabled: Boolean(assessmentId) && enabled,
  });
}
