import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { PublicSpaceDto } from '../model/types';
import { PUBLIC_SPACE_QUERY_KEYS } from './publicSpaceKeys';

/**
 * `DELETE /api/admin/public-space/programs/{programId}` — (ESKI) dastur biriktirmasini olib
 * tashlash. 2026-09-23 dan faqat `programs[].testDefinitionId == null` bo'lgan eski dasturlar
 * uchun (`docs/07` §3.7); test biriktirmasi — `useSetPublicSpaceTest`.
 */
export function useUnassignPublicSpaceProgram() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (programId: string) =>
      adminRequest<PublicSpaceDto>(`/api/admin/public-space/programs/${programId}`, {
        method: 'DELETE',
      }),
    onSuccess: (data) => {
      queryClient.setQueryData(PUBLIC_SPACE_QUERY_KEYS.detail(), data);
    },
  });
}
