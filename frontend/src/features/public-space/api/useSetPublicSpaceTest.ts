import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { PublicSpaceDto } from '../model/types';
import { PUBLIC_SPACE_QUERY_KEYS } from './publicSpaceKeys';

/**
 * `POST /api/admin/public-space/tests/{testId}` — TEST biriktirish (2026-09-23, `docs/07`
 * §3.7; idempotent, test dasturi bo'lmasa backend yaratadi). Javob — yangilangan
 * `AdminPublicSpaceDto`, keshga to'g'ridan-to'g'ri yoziladi. Test ichidagi "Biriktirish"
 * kartasi (`features/catalog`) ham shu holatni ko'rsatadi — uning keshi eskirgan deb belgilanadi.
 */
export function useAssignPublicSpaceTest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (testId: string) =>
      adminRequest<PublicSpaceDto>(`/api/admin/public-space/tests/${testId}`, { method: 'POST' }),
    onSuccess: (data, testId) => {
      queryClient.setQueryData(PUBLIC_SPACE_QUERY_KEYS.detail(), data);
      void queryClient.invalidateQueries({ queryKey: ['catalog', 'tests', 'assignment', testId] });
    },
  });
}

/** `DELETE /api/admin/public-space/tests/{testId}` — test biriktirmasini olib tashlash (idempotent). */
export function useUnassignPublicSpaceTest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (testId: string) =>
      adminRequest<PublicSpaceDto>(`/api/admin/public-space/tests/${testId}`, {
        method: 'DELETE',
      }),
    onSuccess: (data, testId) => {
      queryClient.setQueryData(PUBLIC_SPACE_QUERY_KEYS.detail(), data);
      void queryClient.invalidateQueries({ queryKey: ['catalog', 'tests', 'assignment', testId] });
    },
  });
}
