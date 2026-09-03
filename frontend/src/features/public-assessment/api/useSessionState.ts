import { useQuery } from '@tanstack/react-query';
import { publicRequest } from '@/shared/api/publicClient';
import { QUERY_KEYS } from '@/shared/config/queryKeys';
import type { SessionStateResponse } from '@/shared/api/types';

/**
 * `GET /api/public/sessions/me` — docs/07-api-shartnoma.md, 1.3-bo'lim.
 * `X-Session-Token` `publicClient` tomonidan avtomatik qo'shiladi (`sessionStore`dan).
 * Faqat `enabled: true` bo'lganda (ya'ni tokeni bor sahifalarda) chaqiriladi; `410` kelsa
 * chaqiruvchi tomon (masalan `LandingPage`) `sessionStore.clear()` chaqiradi
 * (docs/10, 4.1-bo'lim).
 */
export interface UseSessionStateOptions {
  /**
   * `'always'` — sahifa mount bo'lganda kesh YANGI bo'lsa ham qayta so'raladi.
   * `FinishPage` uchun SHART: `queryClient` da `staleTime: 30_000`, ya'ni tez o'quvchi
   * barcha bloklarni 30 soniyadan tez yechsa `sessions/me` umuman qayta so'ralmaydi va
   * yakuniy ekran ESKI suratdagi `currentTestCode` bo'yicha allaqachon tugallangan blokka
   * qaytarib yuboradi (`startTest` → `409` → bo'sh skelet). Batafsil: `FinishPage` izohi.
   */
  refetchOnMount?: 'always';
}

export function useSessionState(enabled: boolean, options: UseSessionStateOptions = {}) {
  return useQuery({
    queryKey: QUERY_KEYS.publicSessionMe(),
    queryFn: ({ signal }) =>
      publicRequest<SessionStateResponse>('/api/public/sessions/me', { signal }),
    enabled,
    retry: false,
    ...(options.refetchOnMount ? { refetchOnMount: options.refetchOnMount } : {}),
  });
}
