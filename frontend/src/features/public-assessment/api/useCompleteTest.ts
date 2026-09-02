import { useMutation } from '@tanstack/react-query';
import { publicRequest } from '@/shared/api/publicClient';
import type { CompleteTestResponse } from '@/shared/api/types';

/**
 * `POST /sessions/tests/{testCode}/complete` — docs/07-api-shartnoma.md, 1.7-bo'lim.
 * **P12 hali yozilmoqda** — javob shakli `CompleteTestResponse` (`shared/api/types.ts`) qo'lda,
 * shartnoma bo'yicha yozilgan (`prompts/21` ko'rsatmasi). `400 VALIDATION_ERROR` (`unansweredCount`)
 * — barcha savollar to'ldirilmagan bo'lsa; bizning UI "Keyingi" tugmasini oldindan bloklagani
 * uchun bu holat amalda kam uchraydi, lekin baribir `AppError` sifatida chaqiruvchiga uzatiladi.
 */
export function useCompleteTest() {
  return useMutation({
    mutationFn: (testCode: string) =>
      publicRequest<CompleteTestResponse>(
        `/api/public/sessions/tests/${encodeURIComponent(testCode)}/complete`,
        { method: 'POST' },
      ),
  });
}
