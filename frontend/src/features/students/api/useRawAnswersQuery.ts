import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { RawAnswerDto } from '../model/profileTypes';
import { STUDENTS_QUERY_KEYS } from './studentsKeys';

/**
 * `GET /api/admin/assessments/{id}/answers?testCode=` — docs/07, 3.3-bo'lim, "Xom javoblar"
 * dialogi (P25, 7-band). Faqat dialog ochilganda so'raladi (`enabled`) — audit ma'lumoti
 * og'ir bo'lishi mumkin, profil yuklanishida darhol kerak emas.
 */
export function useRawAnswersQuery(
  assessmentId: string | undefined,
  testCode: string | undefined,
) {
  return useQuery({
    queryKey: STUDENTS_QUERY_KEYS.rawAnswers(assessmentId ?? '', testCode ?? ''),
    queryFn: ({ signal }) =>
      adminRequest<RawAnswerDto[]>(
        `/api/admin/assessments/${assessmentId ?? ''}/answers?testCode=${testCode ?? ''}`,
        { signal },
      ),
    enabled: Boolean(assessmentId) && Boolean(testCode),
  });
}
