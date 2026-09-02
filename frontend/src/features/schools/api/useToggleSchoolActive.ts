import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { SchoolDetailDto } from '../model/types';
import { SCHOOLS_QUERY_KEYS } from './schoolsKeys';

/** `POST /api/admin/schools/{id}/toggle-active` — docs/07, 3.1-bo'lim. */
export function useToggleSchoolActive() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      adminRequest<SchoolDetailDto>(`/api/admin/schools/${id}/toggle-active`, { method: 'POST' }),
    onSuccess: (_data, id) => {
      void queryClient.invalidateQueries({ queryKey: ['schools', 'list'] });
      void queryClient.invalidateQueries({ queryKey: SCHOOLS_QUERY_KEYS.detail(id) });
    },
  });
}
