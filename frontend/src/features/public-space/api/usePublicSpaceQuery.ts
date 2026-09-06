import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { PublicSpaceDto } from '../model/types';
import { PUBLIC_SPACE_QUERY_KEYS } from './publicSpaceKeys';

/**
 * `GET /api/admin/public-space` — makon holati, statistikasi va biriktirilgan dasturlari.
 *
 * `staleTime` ataylab QISQA (10s): sahifadagi mutatsiyalar (dastur biriktirish, natija
 * sozlamasi) darhol javobning yangi nusxasini qaytaradi va `availability` bloki o'sha
 * zahoti qayta hisoblanishi kerak — "dastur o'chirilgan" ogohlantirishi eskirib qolsa
 * admin yana ko'r bo'lib qoladi (2026-09-03 hodisasining ildizi).
 */
const DETAIL_STALE_TIME_MS = 10_000;

export function usePublicSpaceQuery() {
  return useQuery({
    queryKey: PUBLIC_SPACE_QUERY_KEYS.detail(),
    queryFn: ({ signal }) => adminRequest<PublicSpaceDto>('/api/admin/public-space', { signal }),
    staleTime: DETAIL_STALE_TIME_MS,
  });
}
