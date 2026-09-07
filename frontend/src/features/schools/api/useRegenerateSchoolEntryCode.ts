import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { RegenerateEntryCodeResponse } from '../model/types';
import { SCHOOLS_QUERY_KEYS } from './schoolsKeys';

/**
 * `POST /api/admin/schools/{id}/regenerate-entry-code` — docs/07, 3.1-bo'lim. **Xavfli amal**:
 * eski maktab kodi darhol ishlamay qoladi — faqat `RegenerateEntryCodeDialog` tasdig'i orqali
 * chaqiriladi (`useRegenerateSchoolLink` bilan bir xil naqsh).
 */
export function useRegenerateSchoolEntryCode() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      adminRequest<RegenerateEntryCodeResponse>(`/api/admin/schools/${id}/regenerate-entry-code`, {
        method: 'POST',
      }),
    onSuccess: (_data, id) => {
      void queryClient.invalidateQueries({ queryKey: ['schools', 'list'] });
      void queryClient.invalidateQueries({ queryKey: SCHOOLS_QUERY_KEYS.detail(id) });
    },
  });
}
