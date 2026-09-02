import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import { SETTINGS_QUERY_KEYS } from './settingsKeys';

export interface AccountStatus {
  totpEnabled: boolean;
}

/**
 * `GET /api/auth/me` — 2FA kartasidagi joriy holatni (yoqilgan/yoqilmagan) ko'rsatish uchun.
 * `features/auth`dagi `useMeQuery` bilan bir xil endpoint, lekin ataylab **alohida** — feature'lar
 * bir-birini import qilmaydi (docs/10, 2-bo'lim), shu sabab bu yerda o'z nusxasi.
 */
export function useAccountStatusQuery() {
  return useQuery({
    queryKey: SETTINGS_QUERY_KEYS.account(),
    queryFn: ({ signal }) => adminRequest<AccountStatus>('/api/auth/me', { signal }),
    staleTime: 0,
  });
}
