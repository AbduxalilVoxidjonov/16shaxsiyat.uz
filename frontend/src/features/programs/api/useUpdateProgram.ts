import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type {
  AdminProgramDetailWithRegistration,
  UpdateProgramRequestWithRegistration,
} from '../model/types';
import { PROGRAMS_QUERY_KEYS } from './programsKeys';

export interface UpdateProgramInput {
  id: string;
  payload: UpdateProgramRequestWithRegistration;
}

/** `PUT /api/admin/programs/{id}` — `Code`/`Kind`/`IsSystem` o'zgarmaydi. */
export function useUpdateProgram() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: UpdateProgramInput) =>
      adminRequest<AdminProgramDetailWithRegistration>(`/api/admin/programs/${id}`, {
        method: 'PUT',
        body: payload,
      }),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['programs', 'list'] });
      void queryClient.invalidateQueries({ queryKey: PROGRAMS_QUERY_KEYS.detail(variables.id) });
    },
  });
}
