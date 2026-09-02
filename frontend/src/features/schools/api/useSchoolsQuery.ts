import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { PagedResult } from '@/shared/api/types';
import type { SchoolListItemDto, SchoolsListQuery } from '../model/types';
import { SCHOOLS_QUERY_KEYS } from './schoolsKeys';

/** Ro'yxatlar uchun `staleTime` — docs/10, 5.2-bo'lim: "ro'yxatlar 30s". */
const LIST_STALE_TIME_MS = 30_000;

function buildQueryString(query: SchoolsListQuery): string {
  const params = new URLSearchParams();
  if (query.search) params.set('search', query.search);
  if (query.region) params.set('region', query.region);
  if (query.isActive !== undefined) params.set('isActive', String(query.isActive));
  params.set('page', String(query.page));
  params.set('pageSize', String(query.pageSize));
  if (query.sort) params.set('sort', query.sort);
  return params.toString();
}

/** `GET /api/admin/schools` — docs/07, 3.1-bo'lim. */
export function useSchoolsQuery(query: SchoolsListQuery) {
  return useQuery({
    queryKey: SCHOOLS_QUERY_KEYS.list(query),
    queryFn: ({ signal }) =>
      adminRequest<PagedResult<SchoolListItemDto>>(`/api/admin/schools?${buildQueryString(query)}`, {
        signal,
      }),
    staleTime: LIST_STALE_TIME_MS,
    placeholderData: (previous) => previous,
  });
}
