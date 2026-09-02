import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';

/**
 * `DELETE /api/admin/students/{id}?hard=true` — docs/07, 3.2-bo'lim: "`hard=true` — o'quvchi
 * so'rovi bo'yicha". Profil sahifasidagi "⋯ → O'chirish" amali (P25, 1-band) har doim shu
 * `hard=true` bilan chaqiriladi — bu superadmin tomonidan **individual profildan** aniq
 * o'quvchini butunlay o'chirish, `StudentsPage` jadvalidagi keng qamrovli soft-delete emas.
 */
export function useDeleteStudentMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (studentId: string) =>
      adminRequest<void>(`/api/admin/students/${studentId}?hard=true`, { method: 'DELETE' }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['students', 'list'] });
    },
  });
}
