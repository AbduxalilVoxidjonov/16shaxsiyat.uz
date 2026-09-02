import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { SchoolDetailDto, SchoolUpsertRequest } from '../model/types';

/** `POST /api/admin/schools` — docs/07, 3.1-bo'lim. */
export function useCreateSchool() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: SchoolUpsertRequest) =>
      adminRequest<SchoolDetailDto>('/api/admin/schools', { method: 'POST', body: payload }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['schools', 'list'] });
    },
  });
}
