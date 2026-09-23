import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { AssessmentAnswersDto } from '@/shared/api/assessmentAnswersTypes';

/**
 * `GET /api/admin/assessments/{id}/answers` — docs/07, 3.3-bo'lim, "Savolma-savol javoblar
 * va tahlili". Bo'lim yig'ilmagan holda ko'rsatiladi (egasining talabi, 2026-09-23), shu
 * sabab `assessmentId` bo'lishi bilan darhol so'raladi (~190 qator — bog'langan hajm).
 * `enabled = false` — profildagi YIG'ILGAN urinish sarlavhasi (javoblar soni faqat blok
 * ochilgandan keyin ma'lum bo'ladi, oldindan so'rov yuborilmaydi).
 *
 * `testCode` filtri BERILMAYDI: butun sessiya bir marta olinadi va bloklar mijozda
 * guruhlanadi. Sabab — backend `session`/`scales` signallarini baribir butun sessiya
 * bo'yicha hisoblaydi (`ReliabilityCalculator` sessiya darajasida ishlaydi), shu sabab
 * blok bo'yicha alohida so'rov faqat takroriy trafik berardi.
 *
 * `widgets/`da (P52-A, 2026-09-12): `AnswersSection` endi ikki feature'da ochiladi
 * (`features/students` — o'quvchi profili, `features/assessments` — sessiya detali).
 * `docs/10` §2: "`features/*` bir-birini import qilmaydi" — ilgari `STUDENTS_QUERY_KEYS`ga
 * tayangan edi, endi mustaqil kalit bilan shu yerda.
 */
export function useRawAnswersQuery(assessmentId: string | undefined, enabled = true) {
  return useQuery({
    queryKey: ['assessmentAnswers', assessmentId ?? ''] as const,
    queryFn: ({ signal }) =>
      adminRequest<AssessmentAnswersDto>(
        `/api/admin/assessments/${assessmentId ?? ''}/answers`,
        { signal },
      ),
    enabled: Boolean(assessmentId) && enabled,
  });
}
