import { useMutation } from '@tanstack/react-query';
import { publicUserRequest } from '@/shared/api/publicUserClient';
import type { StartPublicSessionRequestBody, StartSessionResponse } from '@/shared/api/types';

/**
 * `POST /api/me/sessions` — maktabsiz sessiya ochish (`docs/07` §5.4).
 *
 * Javob **aynan** maktab oqimidagi `StartSessionResult`: shundan keyin foydalanuvchi
 * o'sha `X-Session-Token` bilan `features/public-assessment` oqimida davom etadi —
 * parallel test oqimi YO'Q.
 */
export function useStartOwnSession() {
  return useMutation({
    mutationFn: (payload: StartPublicSessionRequestBody) =>
      publicUserRequest<StartSessionResponse>('/api/me/sessions', {
        method: 'POST',
        body: payload,
      }),
  });
}
