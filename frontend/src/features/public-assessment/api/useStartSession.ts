import { useMutation } from '@tanstack/react-query';
import { publicRequest } from '@/shared/api/publicClient';
import type { StartSessionRequestBody, StartSessionResponse } from '@/shared/api/types';

/**
 * `POST /api/public/sessions` — docs/07-api-shartnoma.md, 1.2-bo'lim. Anketa + sessiya ochish.
 * Muvaffaqiyatsiz javoblar (`400`/`409`/`429`) `AppError` sifatida uloqtiriladi — sahifa
 * `error instanceof AppError` bilan `code` ga qarab foydalanuvchiga tushunarli xabar tanlaydi.
 */
export function useStartSession() {
  return useMutation({
    mutationFn: (payload: StartSessionRequestBody) =>
      publicRequest<StartSessionResponse>('/api/public/sessions', {
        method: 'POST',
        body: payload,
      }),
  });
}
