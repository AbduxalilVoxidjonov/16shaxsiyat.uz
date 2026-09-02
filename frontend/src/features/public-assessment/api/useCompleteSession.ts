import { useMutation } from '@tanstack/react-query';
import { publicRequest } from '@/shared/api/publicClient';
import type { CompleteSessionResponse } from '@/shared/api/types';

/**
 * `POST /sessions/complete` — docs/07-api-shartnoma.md, 1.8-bo'lim. **P12 hali yozilmoqda**
 * (`shared/api/types.ts`dagi `CompleteSessionResponse` izohiga qarang). `FinishPage` bu
 * mutatsiyani boshqa `POST`lar bilan bir xil idempotentlik taxminiga tayanib har mount'da
 * (shu jumladan sahifa yangilanganda) chaqiradi — javobning o'zi ("Analyzing"/xabar/
 * `showResultToStudent`) ekranni boshqaradi.
 */
export function useCompleteSession() {
  return useMutation({
    mutationFn: () =>
      publicRequest<CompleteSessionResponse>('/api/public/sessions/complete', { method: 'POST' }),
  });
}
