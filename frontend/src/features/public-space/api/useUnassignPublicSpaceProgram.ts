import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { PublicSpaceDto } from '../model/types';
import { PUBLIC_SPACE_QUERY_KEYS } from './publicSpaceKeys';

/** `DELETE /api/admin/public-space/programs/{programId}` — biriktirishni olib tashlash. */
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
