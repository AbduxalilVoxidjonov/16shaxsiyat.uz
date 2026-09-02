import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';

/**
 * `DELETE /api/admin/schools/{id}` — docs/07, 3.1-bo'lim (soft delete). O'quvchisi bo'lgan
 * maktabda `409` qaytadi — bu hook xatoni o'zgartirmasdan uzatadi, tushunarli xabarga
 * aylantirish chaqiruvchida (`DeleteSchoolDialog`, `SCHOOL_ERROR_CODES.hasStudents`).
 */
export function useDeleteSchool() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => adminRequest<void>(`/api/admin/schools/${id}`, { method: 'DELETE' }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['schools', 'list'] });
    },
  });
}
