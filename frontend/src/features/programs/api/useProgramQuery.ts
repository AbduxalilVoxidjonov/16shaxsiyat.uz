import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { AdminProgramDetailWithRegistration } from '../model/types';
import { PROGRAMS_QUERY_KEYS } from './programsKeys';

/** `GET /api/admin/programs/{id}`. `id === null` bo'lsa so'ralmaydi. */
export function useProgramQuery(id: string | null) {
  return useQuery({
    queryKey: PROGRAMS_QUERY_KEYS.detail(id ?? ''),
    queryFn: ({ signal }) =>
      adminRequest<AdminProgramDetailWithRegistration>(`/api/admin/programs/${id!}`, { signal }),
    enabled: Boolean(id),
  });
}
