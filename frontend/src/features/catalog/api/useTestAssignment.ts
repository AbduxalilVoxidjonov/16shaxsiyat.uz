import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { components } from '@/shared/api/schema';
import {
  normalizeTestAssignment,
  type UpdateTestAssignmentRequest,
} from '../model/assignmentTypes';
import { CATALOG_QUERY_KEYS } from './catalogKeys';

type RawAssignment = components['schemas']['AdminTestAssignmentDto'];

/** `GET /api/admin/catalog/tests/{id}/assignment` — `docs/07` §3.4.1. */
export function useTestAssignmentQuery(testId: string | null) {
  return useQuery({
    queryKey: CATALOG_QUERY_KEYS.assignment(testId ?? ''),
    queryFn: async ({ signal }) =>
      normalizeTestAssignment(
        await adminRequest<RawAssignment>(`/api/admin/catalog/tests/${testId!}/assignment`, {
          signal,
        }),
      ),
    enabled: Boolean(testId),
    retry: false,
  });
}

export interface UpdateTestAssignmentInput {
  testId: string;
  payload: UpdateTestAssignmentRequest;
}

/**
 * `PUT /api/admin/catalog/tests/{id}/assignment` — javob yangilangan DTO, keshga to'g'ridan-
 * to'g'ri yoziladi. Biriktirma boshqa bo'limlarga ham ta'sir qiladi: katalogdagi
 * `usedInProgramCount`, maktablarning havola holati (`linkHealth`) va ommaviy makon ro'yxati —
 * shu sabab ular ham yangilanadi (kalitlar literal: feature'lar bir-birini import qilmaydi,
 * `docs/10` §2).
 */
export function useUpdateTestAssignment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ testId, payload }: UpdateTestAssignmentInput) =>
      normalizeTestAssignment(
        await adminRequest<RawAssignment>(`/api/admin/catalog/tests/${testId}/assignment`, {
          method: 'PUT',
          body: payload,
        }),
      ),
    onSuccess: (data, variables) => {
      queryClient.setQueryData(CATALOG_QUERY_KEYS.assignment(variables.testId), data);
      void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.list() });
      void queryClient.invalidateQueries({ queryKey: ['schools'] });
      void queryClient.invalidateQueries({ queryKey: ['public-space'] });
    },
  });
}
