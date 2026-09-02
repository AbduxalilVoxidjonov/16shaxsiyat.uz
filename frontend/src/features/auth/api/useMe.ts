import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import { AUTH_QUERY_KEYS } from './authKeys';
import type { MeResponse } from '../model/types';

/**
 * `GET /api/auth/me` — docs/07-api-shartnoma.md, 2-bo'lim.
 *
 * Sahifa yangilanganda sessiyani tiklash uchun ishlatiladi (`ProtectedRoute`). Access token
 * xotirada saqlanmagani sabab (`docs/10`, 5.1-bo'lim) birinchi so'rovda odatda token yo'q —
 * `adminRequest` shu holatda `401` oladi va **avtomatik** `POST /api/auth/refresh`ni
 * (`httpOnly` cookie orqali) chaqirib, muvaffaqiyatli bo'lsa so'rovni bir marta qayta yuboradi
 * (`adminClient.ts` mutex mantig'i) — shu tabiiy oqim orqali sessiya tiklanadi, alohida
 * "bootstrap" endpoint kerak emas.
 */
export function useMeQuery(enabled: boolean) {
  return useQuery({
    queryKey: AUTH_QUERY_KEYS.me(),
    queryFn: ({ signal }) => adminRequest<MeResponse>('/api/auth/me', { signal }),
    enabled,
    retry: false,
    staleTime: 0,
  });
}
