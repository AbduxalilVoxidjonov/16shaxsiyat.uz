import { useMutation } from '@tanstack/react-query';
import { publicRequest } from '@/shared/api/publicClient';
import type { StartSessionResponse } from '@/shared/api/types';
import type { StartSessionPayload } from '@/shared/api/registrationModeTypes';

/**
 * `POST /api/public/sessions` — docs/07-api-shartnoma.md, 1.2-bo'lim. Anketa + sessiya ochish.
 * Muvaffaqiyatsiz javoblar (`400`/`409`/`429`) `AppError` sifatida uloqtiriladi — sahifa
 * `error instanceof AppError` bilan `code` ga qarab foydalanuvchiga tushunarli xabar tanlaydi.
 *
 * `StartSessionPayload` — `Full` (mavjud to'liq shakl, `RegistrationPage`) YOKI `None`
 * (shaxs maydonlarisiz anonim shakl, `LandingPage`) so'rov tanasi (P52, `docs/18` §9).
 */
export function useStartSession() {
  return useMutation({
    mutationFn: (payload: StartSessionPayload) =>
      publicRequest<StartSessionResponse>('/api/public/sessions', {
        method: 'POST',
        body: payload,
      }),
  });
}
