import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { PagedResult } from '@/shared/api/types';
import type { PublicSpaceUserDto, PublicSpaceUsersQuery } from '../model/types';
import { PUBLIC_SPACE_QUERY_KEYS } from './publicSpaceKeys';

/** Ro'yxatlar uchun `staleTime` — docs/10, 5.2-bo'lim: "ro'yxatlar 30s". */
const LIST_STALE_TIME_MS = 30_000;

/** `GET /api/admin/public-space/users` query-string — `docs/07` 3.7-bo'lim. */
export function buildPublicSpaceUsersQueryString(query: PublicSpaceUsersQuery): string {
  const params = new URLSearchParams();
  if (query.search) params.set('search', query.search);
  // `all` — backend standarti, yuborilmaydi (URL toza qoladi).
  if (query.status && query.status !== 'all') params.set('status', query.status);
  params.set('page', String(query.page));
  params.set('pageSize', String(query.pageSize));
  if (query.sort) params.set('sort', query.sort);
  return params.toString();
}

/**
 * Ommaviy makonda ro'yxatdan o'tgan foydalanuvchilar — server-side sahifalash/qidiruv/holat
 * filtri (`useStudentsQuery` bilan bir xil naqsh). `placeholderData` — sahifa almashganda
 * jadval "sakrab" bo'shab qolmasin.
 */
export function usePublicSpaceUsersQuery(query: PublicSpaceUsersQuery) {
  return useQuery({
    queryKey: PUBLIC_SPACE_QUERY_KEYS.users(query),
    queryFn: ({ signal }) =>
      adminRequest<PagedResult<PublicSpaceUserDto>>(
        `/api/admin/public-space/users?${buildPublicSpaceUsersQueryString(query)}`,
        { signal },
      ),
    staleTime: LIST_STALE_TIME_MS,
    placeholderData: (previous) => previous,
  });
}
