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
  /**
   * Store'dagi FAOL sessiya o'rniga aniq token bilan so'rash — `LandingPage` havola bilan
   * kelganda (`?k=`) faol sessiya tozalangan, oldingi sessiya esa faqat "Davom ettirish"
   * TAKLIFI (`sessionStore.resumable`) sifatida turadi; u hali tirikmi (410 emasmi) deb
   * tekshirish uchun tokeni shu yerdan uzatiladi. `publicClient` store'da token bo'lsa uni
   * ustun qo'yadi — bu holatda store bo'sh, shu sabab aynan shu token ketadi.
   */
  sessionToken?: string;
}

/** `docs/07` 0-bo'lim — ommaviy API'da sessiya faqat shu header orqali (`publicClient` bilan bir xil nom). */
const SESSION_TOKEN_HEADER = 'X-Session-Token';

export function useSessionState(enabled: boolean, options: UseSessionStateOptions = {}) {
  const explicitToken = options.sessionToken;
  return useQuery({
    queryKey: QUERY_KEYS.publicSessionMe(),
    queryFn: ({ signal }) =>
      publicRequest<SessionStateResponse>('/api/public/sessions/me', {
        signal,
        ...(explicitToken ? { headers: { [SESSION_TOKEN_HEADER]: explicitToken } } : {}),
      }),
    enabled,
    retry: false,
    ...(options.refetchOnMount ? { refetchOnMount: options.refetchOnMount } : {}),
  });
}
