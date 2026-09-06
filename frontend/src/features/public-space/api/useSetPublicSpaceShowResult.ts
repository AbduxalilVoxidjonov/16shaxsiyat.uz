import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { PublicSpaceDto, SetShowResultRequestBody } from '../model/types';
import { PUBLIC_SPACE_QUERY_KEYS } from './publicSpaceKeys';

/**
 * `PUT /api/admin/public-space/show-result` — natijani foydalanuvchiga ko'rsatish bayrog'i.
 *
 * Ilgari bu qiymat FAQAT baza orqali o'zgarardi (`School.SetShowResultToStudent` domen
 * metodi bor edi, lekin uni chaqiradigan endpoint yo'q edi).
 */
export function useSetPublicSpaceShowResult() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (enabled: boolean) =>
      adminRequest<PublicSpaceDto>('/api/admin/public-space/show-result', {
        method: 'PUT',
        // `apiRequest` tanani o'zi JSON qiladi (`client.ts`) — bu yerda XOM obyekt beriladi.
        body: { enabled } satisfies SetShowResultRequestBody,
      }),
    onSuccess: (data) => {
      queryClient.setQueryData(PUBLIC_SPACE_QUERY_KEYS.detail(), data);
    },
  });
}
