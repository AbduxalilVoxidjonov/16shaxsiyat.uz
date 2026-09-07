import { useMutation } from '@tanstack/react-query';
import { apiRequest } from '@/shared/api/client';
import type { ResolveSchoolCodeRequestBody, ResolveSchoolCodeResponse } from '@/shared/api/types';

/**
 * `POST /api/public/schools/resolve-code` — `docs/07` §1.1a. Maktab kodi → `{ slug, accessToken }`,
 * so'ng mijoz MAVJUD maktab oqimiga (`/t/{slug}?k=`) o'tadi.
 *
 * Kod TANADA (URL'da emas — log/tarixga tushmasin). Hech qanday token yo'q — `useTelegramLogin`
 * kabi to'g'ridan-to'g'ri `apiRequest`; cookie kerak emas.
 */
export function useResolveSchoolCode() {
  return useMutation({
    mutationFn: (code: string) =>
      apiRequest<ResolveSchoolCodeResponse>('/api/public/schools/resolve-code', {
        method: 'POST',
        body: { code } satisfies ResolveSchoolCodeRequestBody,
      }),
  });
}
