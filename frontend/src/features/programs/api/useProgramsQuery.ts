import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { PagedResult } from '@/shared/api/types';
import type { AdminProgramListItem, ProgramsListQuery } from '../model/types';
import { PROGRAMS_QUERY_KEYS } from './programsKeys';

const LIST_STALE_TIME_MS = 30_000;

function buildQueryString(query: ProgramsListQuery): string {
  const params = new URLSearchParams();
  if (query.search) params.set('search', query.search);
  if (query.state) params.set('state', query.state);
  params.set('page', String(query.page));
  params.set('pageSize', String(query.pageSize));
  if (query.sort) params.set('sort', query.sort);
  return params.toString();
}

/** `GET /api/admin/programs`. */
export function useProgramsQuery(query: ProgramsListQuery) {
  return useQuery({
    queryKey: PROGRAMS_QUERY_KEYS.list(query),
    queryFn: ({ signal }) =>
      adminRequest<PagedResult<AdminProgramListItem>>(
        `/api/admin/programs?${buildQueryString(query)}`,
        {
          signal,
        },
      ),
    staleTime: LIST_STALE_TIME_MS,
    placeholderData: (previous) => previous,
  });
}
