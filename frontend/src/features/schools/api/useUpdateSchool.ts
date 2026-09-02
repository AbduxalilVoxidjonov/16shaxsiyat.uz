import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { SchoolDetailDto, SchoolUpsertRequest } from '../model/types';
import { SCHOOLS_QUERY_KEYS } from './schoolsKeys';

export interface UpdateSchoolInput {
  id: string;
  payload: SchoolUpsertRequest;
}

/** `PUT /api/admin/schools/{id}` — docs/07, 3.1-bo'lim. */
export function useUpdateSchool() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: UpdateSchoolInput) =>
      adminRequest<SchoolDetailDto>(`/api/admin/schools/${id}`, { method: 'PUT', body: payload }),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['schools', 'list'] });
      void queryClient.invalidateQueries({ queryKey: SCHOOLS_QUERY_KEYS.detail(variables.id) });
    },
  });
}
