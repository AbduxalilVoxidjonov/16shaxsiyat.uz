import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { AdminProgramDetail } from '../model/types';
import { PROGRAMS_QUERY_KEYS } from './programsKeys';

export interface ProgramSchoolInput {
  programId: string;
  schoolId: string;
}

/** `POST /api/admin/programs/{id}/schools/{schoolId}` — idempotent biriktirish. */
export function useAssignProgramSchool() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ programId, schoolId }: ProgramSchoolInput) =>
      adminRequest<AdminProgramDetail>(`/api/admin/programs/${programId}/schools/${schoolId}`, {
        method: 'POST',
      }),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['programs', 'list'] });
      void queryClient.invalidateQueries({
        queryKey: PROGRAMS_QUERY_KEYS.detail(variables.programId),
      });
    },
  });
}

/** `DELETE /api/admin/programs/{id}/schools/{schoolId}` — idempotent olib tashlash. */
export function useUnassignProgramSchool() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ programId, schoolId }: ProgramSchoolInput) =>
      adminRequest<AdminProgramDetail>(`/api/admin/programs/${programId}/schools/${schoolId}`, {
        method: 'DELETE',
      }),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['programs', 'list'] });
      void queryClient.invalidateQueries({
        queryKey: PROGRAMS_QUERY_KEYS.detail(variables.programId),
      });
    },
  });
}
