import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { AdminProgramDetail, CreateProgramRequestBody } from '../model/types';

/** `POST /api/admin/programs` — har doim `Kind = Custom`/`Status = Draft`. */
export function useCreateProgram() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateProgramRequestBody) =>
      adminRequest<AdminProgramDetail>('/api/admin/programs', { method: 'POST', body: payload }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['programs', 'list'] });
    },
  });
}
