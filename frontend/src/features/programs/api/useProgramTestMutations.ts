import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type {
  AddProgramTestRequestBody,
  AdminProgramDetail,
  ReorderProgramTestsRequestBody,
} from '../model/types';
import { PROGRAMS_QUERY_KEYS } from './programsKeys';

function invalidateProgram(queryClient: ReturnType<typeof useQueryClient>, id: string) {
  void queryClient.invalidateQueries({ queryKey: ['programs', 'list'] });
  void queryClient.invalidateQueries({ queryKey: PROGRAMS_QUERY_KEYS.detail(id) });
}

export interface AddProgramTestInput {
  programId: string;
  payload: AddProgramTestRequestBody;
}

/** `POST /api/admin/programs/{id}/tests` — tizim dasturida `409 SYSTEM_PROGRAM_LOCKED`. */
export function useAddProgramTest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ programId, payload }: AddProgramTestInput) =>
      adminRequest<AdminProgramDetail>(`/api/admin/programs/${programId}/tests`, {
        method: 'POST',
        body: payload,
      }),
    onSuccess: (_data, variables) => {
      invalidateProgram(queryClient, variables.programId);
    },
  });
}

export interface RemoveProgramTestInput {
  programId: string;
  testDefinitionId: string;
}

/** `DELETE /api/admin/programs/{id}/tests/{testDefinitionId}` — tizim dasturida `409 SYSTEM_PROGRAM_LOCKED`. */
export function useRemoveProgramTest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ programId, testDefinitionId }: RemoveProgramTestInput) =>
      adminRequest<AdminProgramDetail>(
        `/api/admin/programs/${programId}/tests/${testDefinitionId}`,
        {
          method: 'DELETE',
        },
      ),
    onSuccess: (_data, variables) => {
      invalidateProgram(queryClient, variables.programId);
    },
  });
}

export interface ReorderProgramTestsInput {
  programId: string;
  payload: ReorderProgramTestsRequestBody;
}

/** `POST /api/admin/programs/{id}/tests/reorder` — tizim dasturida `409 SYSTEM_PROGRAM_LOCKED`. */
export function useReorderProgramTests() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ programId, payload }: ReorderProgramTestsInput) =>
      adminRequest<AdminProgramDetail>(`/api/admin/programs/${programId}/tests/reorder`, {
        method: 'POST',
        body: payload,
      }),
    onSuccess: (_data, variables) => {
      invalidateProgram(queryClient, variables.programId);
    },
  });
}
