import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { PublicSpaceDto } from '../model/types';
import { PUBLIC_SPACE_QUERY_KEYS } from './publicSpaceKeys';

/**
 * `POST /api/admin/public-space/programs/{programId}` — dastur biriktirish.
 *
 * Backend mavjud `school_programs` mexanizmini QAYTA ISHLATADI (nusxa yo'q), shu sabab bu
 * yerda ham alohida shakl emas, o'sha yangilangan `AdminPublicSpaceDto` qaytadi va keshga
 * to'g'ridan-to'g'ri yoziladi (qo'shimcha `GET` so'rovisiz).
 */
export function useAssignPublicSpaceProgram() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (programId: string) =>
      adminRequest<PublicSpaceDto>(`/api/admin/public-space/programs/${programId}`, {
        method: 'POST',
      }),
    onSuccess: (data) => {
      queryClient.setQueryData(PUBLIC_SPACE_QUERY_KEYS.detail(), data);
    },
  });
}
